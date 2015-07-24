using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA;
using FISCA.Presentation;
using FISCA.Permission;
using Campus.IRewrite.Interface;

namespace K12.Club.General.Interfacing.KH
{
    public class Program
    {
        [MainMethod()]
        static public void Main()
        {

            FISCA.InteractionService.RegisterAPI<IClubClearingFormAPI>(new Clearing());


            #region 學期結算

            //這是一個For高雄使用的社團模組外掛
            //使用者掛載此模組,可以使用到高雄相關的功能
            //1.社團結算成績,匯出功能
            //包含努力程度換算?

            //2.高雄社團資料項目
            //內容為呈現高雄社團的社團成績
            //此欄位是相關報表真正使用的成績內容

            //RibbonBarItem totle2 = FISCA.Presentation.MotherForm.RibbonBarItems["通用社團", "成績"];
            //totle2["學期結算(高雄)"].Enable = false;
            //totle2["學期結算(高雄)"].Image = Properties.Resources.brand_write_64;
            //totle2["學期結算(高雄)"].Click += delegate
            //{
            //    //本功能將會把學生之成績進行結算至高雄版本社團之成績內
            //    //儲存成績&努力程度

            //    //高雄社團成績欄位
            //    ClearingForm cf = new ClearingForm();
            //    cf.ShowDialog();
            //};


            //ClubAdmin.Instance.SelectedSourceChanged += delegate
            //{
            //    if (ClubAdmin.Instance.SelectedSource.Count > 0 && Permissions.學期結算_For高雄權限)
            //        totle2["學期結算(高雄)"].Enable = true;
            //    else
            //        totle2["學期結算(高雄)"].Enable = false;
            //};

            #endregion

            //學生社團成績
            //FeatureAce UserPermission = FISCA.Permission.UserAcl.Current[Permissions.社團記錄_For高雄];
            //if (UserPermission.Editable || UserPermission.Viewable)
            //    K12.Presentation.NLDPanels.Student.AddDetailBulider(new FISCA.Presentation.DetailBulider<StudentSettleAccountsAssnItem>());

            //Catalog detail1;
            //detail1 = RoleAclSource.Instance["社團"]["功能按鈕"];
            //detail1.Add(new RibbonFeature(Permissions.學期結算_For高雄, "學期結算(高雄)"));

            //detail1 = RoleAclSource.Instance["學生"]["資料項目"];
            //detail1.Add(new DetailItemFeature(Permissions.社團記錄_For高雄, "社團成績(高雄)"));
        }
    }
}
