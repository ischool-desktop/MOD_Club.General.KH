using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace K12.Club.General.Interfacing.KH
{
    public class 一名學生
    {
        public enum 男女 { 男, 女, 不限制 }
        //學生
        public string student_id { get; set; }
        public string status { get; set; }
        public string student_name { get; set; }
        public int? seat_no { get; set; }
        public 男女 gender { get; set; }
        //班級
        public string class_id { get; set; }
        public string class_name { get; set; }
        public string display_order { get; set; }
        public string grade_year { get; set; }
        //教師
        public string teacher_id { get; set; }
        public string teacher_name { get; set; }
        public string nickname { get; set; }

        public 一名學生(DataRow row)
        {
            student_id = "" + row["student_id"];
            status = "" + row["status"];
            student_name = "" + row["student_name"];

            #region 性別
            if ("" + row["gender"] == "1")
            {
                gender = 男女.男;
            }
            else if ("" + row["gender"] == "0")
            {
                gender = 男女.女;
            }
            else
            {
                gender = 男女.不限制;
            } 
            #endregion

            #region 座號
            int a;
            if (int.TryParse("" + row["seat_no"], out a))
            {
                seat_no = a;
            }
            #endregion

            class_id = "" + row["class_id"];
            class_name = "" + row["class_name"];
            display_order = "" + row["display_order"];
            grade_year = "" + row["grade_year"];

            teacher_id = "" + row["teacher_id"];
            teacher_name = "" + row["teacher_name"];
            nickname = "" + row["nickname"];
        }
    }
}
