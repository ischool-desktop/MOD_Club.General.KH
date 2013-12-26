using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA.UDT;

namespace K12.Club.General.Interfacing.KH
{
    class OldAssociationsResults
    {
        public List<AssnCode> InsertScoreList_ByKH { get; set; }

        //public List<AssnCode> UPDateScoreList_ByKH { get; set; }

        public List<AssnCode> DeleteScoreList_ByKH { get; set; }

        AccessHelper _access = new AccessHelper();

        public OldAssociationsResults(List<string> StudentIDList, string _SchoolYear, string _Semester)
        {
            //UPDateScoreList_ByKH = new List<AssnCode>();
            InsertScoreList_ByKH = new List<AssnCode>();
            DeleteScoreList_ByKH = new List<AssnCode>();

            //取得高雄社團成績 / 學年度學期
            //有資料表示是重覆資料,需將其移除
            string qu = string.Join("','", StudentIDList);
            DeleteScoreList_ByKH = _access.Select<AssnCode>(string.Format("StudentID in ('{0}') and SchoolYear='{1}' and Semester='{2}'", qu, _SchoolYear, _Semester));
        }
    }
}                         
