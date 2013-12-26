using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevComponents.DotNetBar.Controls;
using DevComponents.DotNetBar;
using FISCA.Data;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using K12.Data;
using Aspose.Cells;
using FISCA.UDT;

namespace K12.Club.General.Interfacing.KH
{
    static class tool
    {
        static public AccessHelper _A = new AccessHelper();
        static public QueryHelper _Q = new QueryHelper();

        /// <summary>
        /// 取得傳入的社團ID清單
        /// (含依據社團序號/社團名稱排序)
        /// </summary>
        static public Dictionary<string, CLUBRecord> GetClub(List<string> ClubIDList)
        {
            Dictionary<string, CLUBRecord> dic = new Dictionary<string, CLUBRecord>();
            List<CLUBRecord> ClubList = tool._A.Select<CLUBRecord>(ClubIDList);
            ClubList.Sort(SortClub);
            foreach (CLUBRecord club in ClubList)
            {
                if (!dic.ContainsKey(club.UID))
                {
                    dic.Add(club.UID, club);
                }
            }
            return dic;
        }

        /// <summary>
        /// 排序社團依據:代碼/名稱排序
        /// </summary>
        static private int SortClub(CLUBRecord cr1, CLUBRecord cr2)
        {
            string Comp1 = cr1.ClubNumber.PadLeft(5, '0');
            Comp1 += cr1.ClubName.PadLeft(20, '0');

            string Comp2 = cr2.ClubNumber.PadLeft(5, '0');
            Comp2 += cr2.ClubName.PadLeft(20, '0');

            return Comp1.CompareTo(Comp2);
        }

        /// <summary>
        /// 確認學生狀態是否正確,
        /// True:一般或延修生
        /// </summary>
        static public bool CheckStatus(StudentRecord student)
        {
            if (student.Status == StudentRecord.StudentStatus.一般 || student.Status == StudentRecord.StudentStatus.延修)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
