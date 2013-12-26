using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FCode = Framework.Security.FeatureCodeAttribute;
using Framework;
using JHSchool.Data;
using System.Xml;
using FISCA.UDT;
using FISCA.DSAUtil;

namespace K12.Club.General.Interfacing.KH
{
    [FCode("K12.Club.Interfacing.ForKaoHsiung.2", "社團成績(高雄)")]
    public partial class StudentSettleAccountsAssnItem : DetailContentBase
    {

        internal static Framework.Security.FeatureAce UserPermission;

        BackgroundWorker BGW = new BackgroundWorker();
        private AccessHelper _accessHelper = new AccessHelper();

        private bool IsBusy = false; //背景是否忙碌
        //private bool SetupReady = false; //設定是否準備完成

        private ChangeListener DataListener { get; set; } //資料變更事件引發器

        private List<AssnCode> UDTAssnList = new List<AssnCode>(); //UDT取得的資料
        private JHAEIncludeRecord newAEInc = new JHAEIncludeRecord(); //社團設定檔

        private Dictionary<string, int> ColumnIndexDic = new Dictionary<string, int>(); //記錄Column欄位Index

        //修改資料判斷用
        List<DataGridViewRow> DataViewList = new List<DataGridViewRow>();

        //錯誤資料判斷用
        List<DataGridViewCell> ErrorCellList = new List<DataGridViewCell>();

        //努力程度操作物件
        private EffortMapper EDT = new EffortMapper();

        //建構子
        public StudentSettleAccountsAssnItem()
        {
            InitializeComponent();

            Group = "社團成績(高雄)";

            UserPermission = User.Acl[FCode.GetCode(GetType())];

            this.Enabled = UserPermission.Editable;

            //背景
            BGW.DoWork += new DoWorkEventHandler(BGW_DoWork);
            BGW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGW_RunWorkerCompleted);

            //當評量設定更新時
            JHAEInclude.AfterUpdate += new EventHandler<K12.Data.DataChangedEventArgs>(JHAEInclude_AfterUpdate);
            JHAEInclude.AfterInsert += new EventHandler<K12.Data.DataChangedEventArgs>(JHAEInclude_AfterUpdate);
            JHAEInclude.AfterDelete += new EventHandler<K12.Data.DataChangedEventArgs>(JHAEInclude_AfterUpdate);

            //畫面更動判斷
            DataListener = new ChangeListener();
            DataListener.Add(new DataGridViewSource(dataGridViewX1));
            DataListener.StatusChanged += new EventHandler<ChangeEventArgs>(DataListener_StatusChanged);

            //BGW.RunWorkerAsync();

            //畫面建構是否完成
            //SetupReady = BingSetup();

            DataListener.SuspendListen(); //終止變更判斷
        }

        //切換學生
        protected override void OnPrimaryKeyChanged(EventArgs e)
        {
            if (this.PrimaryKey != "")
            {
                this.Loading = true;

                if (BGW.IsBusy) //如果是忙碌的
                {
                    IsBusy = true; //為True
                }
                else
                {
                    BGW.RunWorkerAsync(); //否則直接執行
                }
            }
        }

        //如果設定檔更新了
        void JHAEInclude_AfterUpdate(object sender, K12.Data.DataChangedEventArgs e)
        {
            //SetupReady = BingSetup();

            if (this.PrimaryKey != "")
            {
                this.Loading = true;

                if (BGW.IsBusy) //如果是忙碌的
                {
                    IsBusy = true; //為True
                }
                else
                {
                    BGW.RunWorkerAsync(); //否則直接執行
                }
            }
        }

        void BGW_DoWork(object sender, DoWorkEventArgs e)
        {
            UDTAssnList.Clear();
            //取得資料
            UDTAssnList = _accessHelper.Select<AssnCode>(string.Format("StudentID='{0}'", this.PrimaryKey));
            //排序
            UDTAssnList.Sort(new Comparison<AssnCode>(SortAssn));
        }

        void BGW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (IsBusy)
            {
                IsBusy = false;
                BGW.RunWorkerAsync();
                return;
            }

            BingSetup();

            BindData();

            this.Loading = false;
        }


        /// <summary>
        /// 預設DataGridView畫面(依社團評量狀態
        /// </summary>
        private void BingSetup()
        {
            //定位器
            ColumnIndexDic.Clear();
            dataGridViewX1.EndEdit();
            dataGridViewX1.Rows.Clear();
            dataGridViewX1.Columns.Clear();
            DataViewList.Clear();
            ErrorCellList.Clear();

            int SchoolYearIndex = dataGridViewX1.Columns.Add("學年度", "學年度");
            ColumnIndexDic.Add("學年度", SchoolYearIndex);

            int SemesterIndex = dataGridViewX1.Columns.Add("學期", "學期");
            ColumnIndexDic.Add("學期", SemesterIndex);

            int AssnNameIndex = dataGridViewX1.Columns.Add("社團名稱", "社團名稱");
            ColumnIndexDic.Add("社團名稱", AssnNameIndex);

            //判斷評量設定是否存在
            //評量設定:社團評量(社團模組)
            //試別名稱:社團評量
            List<JHAssessmentSetupRecord> AssSetupList = JHAssessmentSetup.SelectAll();
            string RefAssessmentSetupID = "";
            
            foreach (JHAssessmentSetupRecord each in AssSetupList)
            {
                if (each.Name == "社團評量(社團模組)")
                {
                    RefAssessmentSetupID = each.ID;
                    break;
                }
            }

            //如果有設定檔
            if (RefAssessmentSetupID != "")
            {
                List<JHAEIncludeRecord> AEIncList = JHAEInclude.SelectAll();
                foreach (JHAEIncludeRecord each in AEIncList)
                {
                    if (each.RefAssessmentSetupID == RefAssessmentSetupID && each.ExamName == "社團評量")
                    {
                        newAEInc = each;
                        break;
                    }
                }
            }

            //沒有設定檔,就沒有評分樣板
            if (newAEInc != null)
            {
                if (newAEInc.UseScore)
                {
                    int index = dataGridViewX1.Columns.Add("成績", "成績");
                    dataGridViewX1.Columns[index].Width = 60;
                    ColumnIndexDic.Add("成績", index);
                }

                if (newAEInc.UseEffort)
                {
                    int index = dataGridViewX1.Columns.Add("努力程度", "努力程度");
                    dataGridViewX1.Columns[index].Width = 85;
                    ColumnIndexDic.Add("努力程度", index);
                }

                if (newAEInc.UseText)
                {
                    int index = dataGridViewX1.Columns.Add("文字描述", "文字描述");
                    dataGridViewX1.Columns[index].Width = 200;
                    ColumnIndexDic.Add("文字描述", index);
                }
            }
        }

        /// <summary>
        /// 填入UDT取得資料內容
        /// </summary>
        private void BindData()
        {

            //將資料填入畫面
            foreach (AssnCode each in UDTAssnList)
            {
                string SchoolYearMode = each.SchoolYear;
                string SemesterMode = each.Semester;

                if (each.Scores != "") //成績不為空白時
                {
                    XmlElement xml = DSXmlHelper.LoadXml(each.Scores); //將文字轉為XmlElement

                    if (xml.SelectNodes("Item").Count != 0)
                    {
                        foreach (XmlElement node in xml.SelectNodes("Item"))
                        {
                            int RowIndex = SetNullModeInAssn(each);
                            DataGridViewRow row = dataGridViewX1.Rows[RowIndex]; //取得新增的Row

                            row.Cells[ColumnIndexDic["社團名稱"]].Value = node.GetAttribute("AssociationName"); //課程名稱
                            row.Cells[ColumnIndexDic["社團名稱"]].Style.BackColor = Color.LightCyan;
                            row.Cells[ColumnIndexDic["社團名稱"]].ReadOnly = true;

                            if (newAEInc.UseScore) //依設定檔決定顯示內容
                            {
                                row.Cells[ColumnIndexDic["成績"]].Value = node.GetAttribute("Score"); //成績
                            }
                            if (newAEInc.UseEffort)
                            {
                                row.Cells[ColumnIndexDic["努力程度"]].Value = node.GetAttribute("Effort"); //努力程度
                            }
                            if (newAEInc.UseText)
                            {
                                row.Cells[ColumnIndexDic["文字描述"]].Value = node.GetAttribute("Text"); //文字描述
                            }
                        }
                    }
                    else //沒有相關資訊時
                    {
                        SetNullModeInAssn(each);
                    }
                }
                else //如果沒有社團相關資料的物件
                {
                    SetNullModeInAssn(each);
                }
            }

            SaveButtonVisible = false;
            CancelButtonVisible = false;

            //資料更新完畢,開始判斷資料更新狀況
            DataListener.Reset();
            DataListener.ResumeListen(); 
        }

        /// <summary>
        /// 傳入社團物件,直接建立一個基本資料的Row,並回傳建立的RowIndex
        /// </summary>
        private int SetNullModeInAssn(AssnCode ac)
        {
            DataGridViewRow row = new DataGridViewRow();
            row.CreateCells(dataGridViewX1);
            row.Tag = ac;
            row.Cells[ColumnIndexDic["學年度"]].Value = ac.SchoolYear;
            row.Cells[ColumnIndexDic["學年度"]].Style.BackColor = Color.LightCyan;
            row.Cells[ColumnIndexDic["學年度"]].ReadOnly = true;
            row.Cells[ColumnIndexDic["學期"]].Value = ac.Semester;
            row.Cells[ColumnIndexDic["學期"]].Style.BackColor = Color.LightCyan;
            row.Cells[ColumnIndexDic["學期"]].ReadOnly = true;
            return dataGridViewX1.Rows.Add(row);
        }

        //儲存
        protected override void OnSaveButtonClick(EventArgs e)
        {
            dataGridViewX1.EndEdit();

            if (ErrorCellList.Count != 0) //資料檢查
            {
                MsgBox.Show("請修改錯誤資料!!");
                return;
            }

            List<AssnCode> list = new List<AssnCode>();

            foreach (DataGridViewRow each in DataViewList)
            {
                if (each.Tag != null) //如果不為空,就是既有物件
                {
                    AssnCode ac = (AssnCode)each.Tag;

                    XmlElement xml = DSXmlHelper.LoadXml(ac.Scores);

                    foreach (XmlElement node in xml.SelectNodes("Item"))
                    {
                        if ("" + each.Cells[ColumnIndexDic["社團名稱"]].Value == node.GetAttribute("AssociationName"))
                        {
                            #region 修改後更新

                            xml.RemoveChild(node); //移除所選項目
                            DSXmlHelper dsHelper1 = new DSXmlHelper(xml);
                            XmlElement xmlbig = dsHelper1.AddElement("Item");
                            DSXmlHelper dsHelper2 = new DSXmlHelper(xmlbig);

                            dsHelper2.SetAttribute(".", "AssociationName", "" + each.Cells[ColumnIndexDic["社團名稱"]].Value);
                            dsHelper2.SetAttribute(".", "Score", newAEInc.UseScore ? "" + each.Cells[ColumnIndexDic["成績"]].Value : "");
                            dsHelper2.SetAttribute(".", "Effort", newAEInc.UseEffort ? "" + each.Cells[ColumnIndexDic["努力程度"]].Value : "");
                            dsHelper2.SetAttribute(".", "Text", newAEInc.UseText ? "" + each.Cells[ColumnIndexDic["文字描述"]].Value : "");
                            ac.Scores = xml.OuterXml;

                            break;
                            #endregion
                        }
                    }

                    list.Add(ac);
                    _accessHelper.UpdateValues(list.ToArray());
                    list.Clear();
                }
                else //屬於新增欄位
                {
                    string SchoolYear = "" + each.Cells[ColumnIndexDic["學年度"]].Value;
                    string Semester = "" + each.Cells[ColumnIndexDic["學期"]].Value;

                    AssnCode newAC = CheckAssnIsFad(SchoolYear, Semester); //判斷是否重覆

                    if (newAC != null) //是既有資料
                    {
                        string ScordE = newAC.Scores;
                        XmlElement xml = DSXmlHelper.LoadXml(ScordE);
                        DSXmlHelper ds = new DSXmlHelper(xml); //取得Xml結構

                        ds.AddElement("Item");
                        ds.SetAttribute("Item", "AssociationName", "" + each.Cells[ColumnIndexDic["社團名稱"]].Value);
                        ds.SetAttribute("Item", "Score", newAEInc.UseScore ? "" + each.Cells[ColumnIndexDic["成績"]].Value : "");
                        ds.SetAttribute("Item", "Effort", newAEInc.UseEffort ? "" + each.Cells[ColumnIndexDic["努力程度"]].Value : "");
                        ds.SetAttribute("Item", "Text", newAEInc.UseText ? "" + each.Cells[ColumnIndexDic["文字描述"]].Value : "");
                        newAC.Scores = ds.BaseElement.OuterXml;
                        list.Add(newAC);
                        _accessHelper.UpdateValues(list.ToArray());
                        list.Clear();

                    }
                    else //新資料
                    {
                        newAC = new AssnCode();
                        newAC.SchoolYear = SchoolYear;
                        newAC.Semester = Semester;
                        newAC.StudentID = this.PrimaryKey;

                        DSXmlHelper ds = new DSXmlHelper("Content");
                        ds.AddElement("Item");
                        ds.SetAttribute("Item", "AssociationName", "" + each.Cells[ColumnIndexDic["社團名稱"]].Value);
                        ds.SetAttribute("Item", "Score", newAEInc.UseScore ? "" + each.Cells[ColumnIndexDic["成績"]].Value : "");
                        ds.SetAttribute("Item", "Effort", newAEInc.UseEffort ? "" + each.Cells[ColumnIndexDic["努力程度"]].Value : "");
                        ds.SetAttribute("Item", "Text", newAEInc.UseText ? "" + each.Cells[ColumnIndexDic["文字描述"]].Value : "");

                        newAC.Scores = ds.BaseElement.OuterXml;
                        list.Add(newAC);
                        _accessHelper.InsertValues(list.ToArray());
                        list.Clear();
                    }
                }
            }

            SaveButtonVisible = false;
            CancelButtonVisible = false;

            MsgBox.Show("儲存成功!");

            this.Loading = true;

            DataListener.SuspendListen(); //終止變更判斷
            BGW.RunWorkerAsync(); //背景作業,取得並重新填入原資料 
        }

        private AssnCode CheckAssnIsFad(string SchoolYearNew, string SemesterNew)
        {
            foreach (AssnCode each in UDTAssnList)
            {
                if (each.SchoolYear == SchoolYearNew && each.Semester == SemesterNew) //學年度學期如果相等
                {
                    return each;
                }
            }

            return null;
        }

        //取消
        protected override void OnCancelButtonClick(EventArgs e)
        {
            SaveButtonVisible = false;
            CancelButtonVisible = false;

            this.Loading = true;

            DataListener.SuspendListen(); //終止變更判斷

            if (!BGW.IsBusy)
            {
                BGW.RunWorkerAsync();
            }
        }

        //設團成績排序
        private int SortAssn(AssnCode x, AssnCode y)
        {
            string xx = x.SchoolYear.PadLeft(5, '0');
            xx += x.Semester.PadLeft(5, '0');

            string yy = y.SchoolYear.PadLeft(5, '0');
            yy += y.Semester.PadLeft(5, '0');


            return yy.CompareTo(xx); 
        }

        //當Cell結束編輯
        private void dataGridViewX1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            //新行
            if (dataGridViewX1.CurrentRow.IsNewRow)
                return;

            if (dataGridViewX1.CurrentCell.OwningColumn.HeaderText == "成績")
            {
                #region 成績
                string Score = "" + dataGridViewX1.CurrentCell.Value;
                decimal nowScore = 0;
                if (!decimal.TryParse(Score, out nowScore) && Score != "")
                {
                    if (!ErrorCellList.Contains(dataGridViewX1.CurrentCell)) //不包含就Add
                    {
                        ErrorCellList.Add(dataGridViewX1.CurrentCell);
                    }
                    dataGridViewX1.CurrentCell.ErrorText = "成績資料不可為文字";
                    return;
                }
                else
                {
                    if (ErrorCellList.Contains(dataGridViewX1.CurrentCell)) //正確且包含就拿掉
                    {
                        ErrorCellList.Remove(dataGridViewX1.CurrentCell);
                    }
                    dataGridViewX1.CurrentCell.ErrorText = "";
                } 
                #endregion
            }
            else if (dataGridViewX1.CurrentCell.OwningColumn.HeaderText == "努力程度")
            {
                #region 努力程度
                if ("" + dataGridViewX1.CurrentCell.Value != "")
                {
                    int CodeText;
                    if (int.TryParse("" + dataGridViewX1.CurrentCell.Value, out CodeText))
                    {
                        dataGridViewX1.CurrentCell.Value = EDT.GetTextByCode(CodeText);
                    }
                } 
                #endregion
            }
            else if (dataGridViewX1.CurrentCell.OwningColumn.HeaderText == "學年度")
            {
                #region 學年度
                string CellSchoolYear = "" + dataGridViewX1.CurrentCell.Value;
                int IntSchoolYear;
                if (!int.TryParse(CellSchoolYear, out IntSchoolYear)) //True就是錯誤
                {
                    if (!ErrorCellList.Contains(dataGridViewX1.CurrentCell)) //不包含就Add
                    {
                        ErrorCellList.Add(dataGridViewX1.CurrentCell);
                    }
                    dataGridViewX1.CurrentCell.ErrorText = "學年度不可輸入數字以外內容";
                    return;
                }
                else
                {
                    if (ErrorCellList.Contains(dataGridViewX1.CurrentCell)) //正確且包含就拿掉
                    {
                        ErrorCellList.Remove(dataGridViewX1.CurrentCell);
                    }
                    dataGridViewX1.CurrentCell.ErrorText = "";
                } 
                #endregion
            }
            else if (dataGridViewX1.CurrentCell.OwningColumn.HeaderText == "學期")
            {
                #region 學期
                string CellSchoolYear = "" + dataGridViewX1.CurrentCell.Value;

                if (CellSchoolYear != "1" && CellSchoolYear != "2")
                {
                    if (!ErrorCellList.Contains(dataGridViewX1.CurrentCell)) //不包含就Add
                    {
                        ErrorCellList.Add(dataGridViewX1.CurrentCell);
                    }
                    dataGridViewX1.CurrentCell.ErrorText = "學期必須輸入1 或 2";
                    return;
                }
                else
                {
                    if (ErrorCellList.Contains(dataGridViewX1.CurrentCell)) //正確且包含就拿掉
                    {
                        ErrorCellList.Remove(dataGridViewX1.CurrentCell);
                    }
                    dataGridViewX1.CurrentCell.ErrorText = "";
                } 
                #endregion
            }

            if (!DataViewList.Contains(dataGridViewX1.CurrentRow))
            {
                DataViewList.Add(dataGridViewX1.CurrentRow);
            }
        }

        private void dataGridViewX1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            DialogResult dr = MsgBox.Show("本操作將會刪除此社團記錄,確定刪除?", MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2);
            if (dr == DialogResult.Yes)
            {

                DataGridViewRow row = dataGridViewX1.CurrentRow;

                //刪除資料也清除錯誤指定器內的Cell
                foreach (DataGridViewCell each in row.Cells)
                {
                    if (ErrorCellList.Contains(each))
                    {
                        ErrorCellList.Remove(each);
                    }
                }

                AssnCode ac = (AssnCode)row.Tag;

                if (ac == null)
                    return;
                
                XmlElement xml = DSXmlHelper.LoadXml(ac.Scores);


                List<AssnCode> list = new List<AssnCode>();

                foreach (XmlNode node in xml.SelectNodes("Item"))
                {
                    XmlElement xmlNode = (XmlElement)node; //

                    if ("" + row.Cells[2].Value == xmlNode.GetAttribute("AssociationName")) //如果選到該筆資料
                    {
                        xml.RemoveChild(xmlNode); //移除該筆資料
                    }
                }

                if (xml.SelectNodes("Item").Count != 0) //更新
                {
                    ac.Scores = xml.OuterXml;
                    list.Add(ac);
                    _accessHelper.UpdateValues(list.ToArray()); //更新刪除
                }
                else
                {
                    ac.Scores = xml.OuterXml;
                    list.Add(ac);
                    _accessHelper.DeletedValues(list.ToArray()); //更新刪除
                }

                this.Loading = true;

                DataListener.SuspendListen(); //終止變更判斷
                BGW.RunWorkerAsync(); //背景作業,取得並重新填入原資料 
            }
            else
            {
                e.Cancel = true;
            }
        }

        //當DataGridView變更時
        void DataListener_StatusChanged(object sender, ChangeEventArgs e)
        {
            SaveButtonVisible = (e.Status == ValueStatus.Dirty);
            CancelButtonVisible = (e.Status == ValueStatus.Dirty);
        }
    }
}
