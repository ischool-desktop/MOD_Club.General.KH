using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FISCA.Presentation.Controls;
using K12.Data;
using FISCA.UDT;
using FISCA.DSAUtil;
using System.Xml;

namespace K12.Club.General.Interfacing.KH
{
    public partial class ClearingForm : BaseForm
    {

        /// <summary>
        /// UDT資料取得器
        /// </summary>
        private AccessHelper _AccessHelper = new AccessHelper();

        BackgroundWorker BGW = new BackgroundWorker();

        OldAssociationsResults OARList { get; set; }

        List<ResultScoreRecord> InsertScoreList { get; set; }

        List<ResultScoreRecord> UPDateScoreList { get; set; }

        string _SchoolYear { get; set; }
        string _Semester { get; set; }

        public ClearingForm()
        {
            InitializeComponent();
        }

        private void ClearingForm_Load(object sender, EventArgs e)
        {
            BGW.DoWork += new DoWorkEventHandler(BGW_DoWork);
            BGW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGW_RunWorkerCompleted);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!BGW.IsBusy)
            {
                btnStart.Enabled = false;
                this.Text = "學期結算(系統結算中...)";
                BGW.RunWorkerAsync();
            }
            else
            {
                MsgBox.Show("系統忙碌中,稍後再試...");
            }
        }

        void BGW_DoWork(object sender, DoWorkEventArgs e)
        {
            //用背景模式進行資料檢查
            List<string> Check_List = new List<string>();
            List<CLUBRecord> ClubList = _AccessHelper.Select<CLUBRecord>(ClubAdmin.Instance.SelectedSource);
            foreach (CLUBRecord each in ClubList)
            {
                string meg = each.SchoolYear.ToString() + each.Semester.ToString();
                if (!Check_List.Contains(meg))
                {
                    _SchoolYear = each.SchoolYear.ToString();
                    _Semester = each.Semester.ToString();
                    Check_List.Add(meg);
                }
            }

            //必須是相同學年度學期才可以進行結算
            if (Check_List.Count == 1)
            {
                #region 選社社團

                InsertScoreList = new List<ResultScoreRecord>();
                UPDateScoreList = new List<ResultScoreRecord>();

                //取得目前選擇課程
                成績取得器 tool = new 成績取得器();
                tool.GetSCJoinByClubIDList(ClubAdmin.Instance.SelectedSource);

                //取得運算比例
                tool.SetWeightProportion();

                //社團ID : 社團幹部obj
                Dictionary<string, 社團幹部obj> CadreDic = new Dictionary<string, 社團幹部obj>();

                #region 處理幹部記錄

                foreach (CLUBRecord each in tool._ClubDic.Values)
                {
                    if (!CadreDic.ContainsKey(each.UID))
                    {
                        CadreDic.Add(each.UID, new 社團幹部obj(each));
                    }
                }

                string qr = string.Join("','", tool._ClubDic.Keys);
                List<CadresRecord> list = _AccessHelper.Select<CadresRecord>("ref_club_id in ('" + qr + "')");
                foreach (CadresRecord cr in list)
                {
                    if (!CadreDic[cr.RefClubID]._Cadre1.ContainsKey(cr.RefStudentID))
                    {
                        CadreDic[cr.RefClubID]._Cadre1.Add(cr.RefStudentID, cr.CadreName);
                    }
                    else
                    {
                        CadreDic[cr.RefClubID]._Cadre1[cr.RefStudentID] += "," + cr.CadreName;
                    }
                }

                #endregion

                Dictionary<string, ResultScoreRecord> ResultScoreDic = new Dictionary<string, ResultScoreRecord>();

                List<string> list_2 = new List<string>();
                foreach (List<SCJoin> each in tool._SCJoinDic.Values)
                {
                    foreach (SCJoin scj in each)
                    {
                        list_2.Add(scj.UID);
                    }
                }
                string uq = string.Join("','", list_2);

                List<ResultScoreRecord> ResultList = _AccessHelper.Select<ResultScoreRecord>("ref_scjoin_id in ('" + uq + "')");

                foreach (ResultScoreRecord rsr in ResultList)
                {
                    if (!ResultScoreDic.ContainsKey(rsr.RefSCJoinID))
                    {
                        ResultScoreDic.Add(rsr.RefSCJoinID, rsr);
                    }
                }

                //_AccessHelper.DeletedValues(ResultList);

                foreach (List<SCJoin> scjList in tool._SCJoinDic.Values)
                {
                    foreach (SCJoin scj in scjList)
                    {
                        //暫時移除
                        //if (scj.AASScore != null || scj.ARScore != null || scj.FARScore != null || scj.PAScore != null)
                        //{
                        if (ResultScoreDic.ContainsKey(scj.UID))
                        {
                            #region 如果有原資料
                            if (tool._StudentDic.ContainsKey(scj.RefStudentID))
                            {
                                //社團
                                CLUBRecord cr = tool._ClubDic[scj.RefClubID];
                                //學生
                                StudentRecord sr = tool._StudentDic[scj.RefStudentID];
                                //原有社團成績記錄
                                ResultScoreRecord update_rsr = ResultScoreDic[scj.UID];

                                update_rsr.SchoolYear = cr.SchoolYear;
                                update_rsr.Semester = cr.Semester;

                                update_rsr.RefClubID = cr.UID; //社團ID
                                update_rsr.RefStudentID = sr.ID; //學生ID
                                update_rsr.RefSCJoinID = scj.UID; //參與記錄ID

                                update_rsr.ClubName = cr.ClubName;

                                #region 成績
                                if (!string.IsNullOrEmpty(scj.Score))
                                {
                                    update_rsr.ResultScore = tool.GetDecimalValue(scj); //成績
                                }
                                else
                                {
                                    update_rsr.ResultScore = null;
                                }
                                #endregion

                                #region 幹部
                                if (CadreDic.ContainsKey(cr.UID))
                                {
                                    if (CadreDic[cr.UID]._Cadre1.ContainsKey(sr.ID))
                                    {
                                        update_rsr.CadreName = CadreDic[cr.UID]._Cadre1[sr.ID]; //幹部
                                    }
                                    else
                                    {
                                        update_rsr.CadreName = "";
                                    }
                                }
                                else
                                {
                                    update_rsr.CadreName = "";
                                }
                                #endregion

                                UPDateScoreList.Add(update_rsr);
                            }
                            #endregion
                        }
                        else
                        {
                            #region 完全沒有成績記錄
                            if (tool._StudentDic.ContainsKey(scj.RefStudentID))
                            {
                                //社團
                                CLUBRecord cr = tool._ClubDic[scj.RefClubID];
                                //學生
                                StudentRecord sr = tool._StudentDic[scj.RefStudentID];

                                ResultScoreRecord rsr = new ResultScoreRecord();
                                rsr.SchoolYear = cr.SchoolYear;
                                rsr.Semester = cr.Semester;

                                rsr.RefClubID = cr.UID; //社團ID
                                rsr.RefStudentID = sr.ID; //學生ID
                                rsr.RefSCJoinID = scj.UID; //參與記錄ID

                                rsr.ClubName = cr.ClubName;

                                bool check = false;

                                if (!string.IsNullOrEmpty(scj.Score))
                                {
                                    check = true;
                                    rsr.ResultScore = tool.GetDecimalValue(scj); //成績
                                }

                                #region 幹部
                                if (CadreDic.ContainsKey(cr.UID))
                                {
                                    if (CadreDic[cr.UID]._Cadre1.ContainsKey(sr.ID))
                                    {
                                        check = true;
                                        rsr.CadreName = CadreDic[cr.UID]._Cadre1[sr.ID];
                                    }
                                    else
                                    {
                                        rsr.CadreName = "";
                                    }
                                }
                                #endregion

                                if (check)
                                {
                                    InsertScoreList.Add(rsr);
                                }
                            }
                            #endregion
                        }
                    }
                }

                try
                {
                    _AccessHelper.InsertValues(InsertScoreList);
                    _AccessHelper.UpdateValues(UPDateScoreList);
                }
                catch (Exception ex)
                {
                    MsgBox.Show("新增社團成績發生錯誤!!\n" + ex.Message);
                    e.Cancel = true;
                    return;
                }

                #endregion

                #region 社團成績Log處理
                StringBuilder _sbLog = new StringBuilder();
                _sbLog.AppendLine("已進行社團結算：");
                if (InsertScoreList.Count > 0)
                    _sbLog.AppendLine("共新增「" + InsertScoreList.Count + "」筆成績記錄");
                if (UPDateScoreList.Count > 0)
                    _sbLog.AppendLine("共更新「" + UPDateScoreList.Count + "」筆成績記錄");
                _sbLog.AppendLine("");
                _sbLog.AppendLine("簡要明細如下：");
                if (InsertScoreList.Count > 0)
                {
                    foreach (ResultScoreRecord each in InsertScoreList)
                    {
                        if (tool._StudentDic.ContainsKey(each.RefStudentID))
                        {
                            if (string.IsNullOrEmpty(each.CadreName))
                            {
                                StudentRecord sr = tool._StudentDic[each.RefStudentID];
                                string de = each.ResultScore.HasValue ? each.ResultScore.Value.ToString() : "";
                                _sbLog.AppendLine(string.Format("學生「{0}」新增社團成績「{1}」", sr.Name, de));
                            }
                            else
                            {
                                StudentRecord sr = tool._StudentDic[each.RefStudentID];
                                string de = each.ResultScore.HasValue ? each.ResultScore.Value.ToString() : "";
                                _sbLog.AppendLine(string.Format("學生「{0}」新增社團成績「{1}」幹部記錄「{2}」", sr.Name, de, each.CadreName));
                            }
                        }
                    }
                }

                if (UPDateScoreList.Count > 0)
                {
                    foreach (ResultScoreRecord each in UPDateScoreList)
                    {
                        if (tool._StudentDic.ContainsKey(each.RefStudentID))
                        {
                            if (string.IsNullOrEmpty(each.CadreName))
                            {
                                StudentRecord sr = tool._StudentDic[each.RefStudentID];
                                string de = each.ResultScore.HasValue ? each.ResultScore.Value.ToString() : "";
                                _sbLog.AppendLine(string.Format("學生「{0}」更新社團成績「{1}」", sr.Name, de));
                            }
                            else
                            {
                                StudentRecord sr = tool._StudentDic[each.RefStudentID];
                                string de = each.ResultScore.HasValue ? each.ResultScore.Value.ToString() : "";
                                _sbLog.AppendLine(string.Format("學生「{0}」更新社團成績「{1}」幹部記錄「{2}」", sr.Name, de, each.CadreName));
                            }
                        }

                    }
                }

                if (InsertScoreList.Count + UPDateScoreList.Count > 0)
                {
                    try
                    {
                        FISCA.LogAgent.ApplicationLog.Log("社團", "成績結算", _sbLog.ToString());
                    }
                    catch (Exception ex)
                    {
                        MsgBox.Show("上傳Log記錄發生錯誤!!\n" + ex.Message);
                        e.Cancel = true;
                        return;
                    }
                }

                #endregion

                #region 高雄社團成績處理

                List<string> StudentIDList = new List<string>();
                foreach (ResultScoreRecord each in InsertScoreList)
                {
                    if (!StudentIDList.Contains(each.RefStudentID))
                    {
                        StudentIDList.Add(each.RefStudentID);
                    }
                }
                foreach (ResultScoreRecord each in UPDateScoreList)
                {
                    if (!StudentIDList.Contains(each.RefStudentID))
                    {
                        StudentIDList.Add(each.RefStudentID);
                    }
                }

                //取得學生原本的的社團成績
                OARList = new OldAssociationsResults(StudentIDList, _SchoolYear, _Semester);
                if (InsertScoreList.Count > 0)
                {
                    //新增資料
                    OARList.InsertScoreList_ByKH.AddRange(GetAssnCode(InsertScoreList));
                }
                if (UPDateScoreList.Count > 0)
                {
                    //更新之社團資料,也是進行新增動作
                    OARList.InsertScoreList_ByKH.AddRange(GetAssnCode(UPDateScoreList));
                }

                try
                {
                    //由於原始資料刪除,因此只有新增資料
                    _AccessHelper.InsertValues(OARList.InsertScoreList_ByKH);
                    //把原使資料給刪除
                    _AccessHelper.DeletedValues(OARList.DeleteScoreList_ByKH);
                }
                catch (Exception ex)
                {
                    MsgBox.Show("結算高雄成績發生錯誤!!\n" + ex.Message);
                    e.Cancel = true;
                    return;
                }

                #endregion

                #region 高雄社團Log處理

                StringBuilder _sbLogRSR = new StringBuilder();
                _sbLogRSR.AppendLine("高雄社團成績結算：");
                _sbLogRSR.AppendLine("共新增「" + OARList.InsertScoreList_ByKH.Count + "」筆成績記錄");
                _sbLogRSR.AppendLine("共刪除「" + OARList.DeleteScoreList_ByKH.Count + "」筆成績記錄");
                _sbLogRSR.AppendLine("");
                _sbLogRSR.AppendLine("簡要明細如下：");

                if (OARList.InsertScoreList_ByKH.Count > 0)
                {
                    foreach (AssnCode each in OARList.InsertScoreList_ByKH)
                    {

                        if (tool._StudentDic.ContainsKey(each.StudentID))
                        {
                            StudentRecord sr = tool._StudentDic[each.StudentID];

                            DSXmlHelper ds = new DSXmlHelper();
                            ds.Load(each.Scores);
                            string AssociationName = "";
                            string Effort = "";
                            foreach (XmlElement xml in ds.GetElements("Item"))
                            {
                                AssociationName = xml.GetAttribute("AssociationName");
                                Effort = xml.GetAttribute("Effort");
                            }

                            _sbLogRSR.AppendLine(string.Format("學生「{0}」新增「{1}」成績努力程度「{2}」", sr.Name, AssociationName, Effort));
                        }
                    }
                }

                if (OARList.DeleteScoreList_ByKH.Count > 0)
                {
                    foreach (AssnCode each in OARList.DeleteScoreList_ByKH)
                    {
                        if (tool._StudentDic.ContainsKey(each.StudentID))
                        {
                            StudentRecord sr = tool._StudentDic[each.StudentID];

                            DSXmlHelper ds = new DSXmlHelper();
                            ds.Load(each.Scores);
                            string AssociationName = "";
                            string Effort = "";
                            foreach (XmlElement xml in ds.GetElements("Item"))
                            {
                                AssociationName = xml.GetAttribute("AssociationName");
                                Effort = xml.GetAttribute("Effort");
                            }

                            _sbLogRSR.AppendLine(string.Format("學生「{0}」刪除「{1}」成績努力程度「{2}」", sr.Name, AssociationName, Effort));

                        }
                    }
                }

                if (OARList.InsertScoreList_ByKH.Count + OARList.DeleteScoreList_ByKH.Count > 0)
                {
                    try
                    {
                        FISCA.LogAgent.ApplicationLog.Log("社團", "高雄社團結算", _sbLogRSR.ToString());
                    }
                    catch (Exception ex)
                    {
                        MsgBox.Show("上傳高雄社團成績Log記錄發生錯誤!!\n" + ex.Message);
                        e.Cancel = true;
                        return;
                    }
                }
                #endregion
            }
            else
            {
                MsgBox.Show("必須是相同學年期之社團才可進行結算!");
                e.Cancel = true;
            }
        }

        void BGW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Text = "學期結算";
            btnStart.Enabled = true;

            if (e.Cancelled)
            {
                FISCA.Presentation.Controls.MsgBox.Show("已中斷結算作業!!");
                return;
            }
            else
            {
                if (e.Error == null)
                {
                    if (InsertScoreList.Count + UPDateScoreList.Count > 0)
                    {
                        StringBuilder sb_8 = new StringBuilder();
                        sb_8.AppendLine("結算完成!!");
                        if (InsertScoreList.Count != 0)
                            sb_8.AppendLine("新增[" + InsertScoreList.Count + "]筆");
                        if (UPDateScoreList.Count != 0)
                            sb_8.AppendLine("更新[" + UPDateScoreList.Count + "]筆");
                        //if (DeleteScoreList.Count != 0)
                        //    sb_8.AppendLine("刪除[" + DeleteScoreList.Count + "]筆");
                        FISCA.Presentation.Controls.MsgBox.Show(sb_8.ToString());
                    }
                    else
                    {
                        FISCA.Presentation.Controls.MsgBox.Show("結算失敗!!無成績記錄可供結算!!");
                    }
                }
                else
                {
                    FISCA.Presentation.Controls.MsgBox.Show("結算發生錯誤!!\n" + e.Error.Message);
                }
            }
        }

        /// <summary>
        /// 取得換算後社團成績
        /// </summary>
        private IEnumerable<AssnCode> GetAssnCode(List<ResultScoreRecord> InsertScoreList)
        {
            List<AssnCode> list = new List<AssnCode>();
            foreach (ResultScoreRecord each in InsertScoreList)
            {
                //有成績才進行結算
                if (each.ResultScore.HasValue)
                {
                    AssnCode ac = new AssnCode();
                    ac.StudentID = each.RefStudentID;
                    ac.SchoolYear = _SchoolYear;
                    ac.Semester = _Semester;

                    DSXmlHelper ds = new DSXmlHelper("Content");
                    ds.AddElement("Item");
                    ds.SetAttribute("Item", "AssociationName", each.ClubName);
                    ds.SetAttribute("Item", "Score", "");
                    //僅換算努力程度
                    ds.SetAttribute("Item", "Effort", GetEfforMapper(each.ResultScore.Value));
                    ds.SetAttribute("Item", "Text", "");
                    ac.Scores = ds.BaseElement.OuterXml;

                    list.Add(ac);
                }
            }
            return list;
        }

        /// <summary>
        /// 進行努力程度換算
        /// </summary>
        private string GetEfforMapper(decimal p)
        {
            EffortMapper _EffortMapper = new EffortMapper();
            return _EffortMapper.GetTextByScore(p);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
