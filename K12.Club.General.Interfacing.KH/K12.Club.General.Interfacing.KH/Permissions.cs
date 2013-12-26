using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace K12.Club.General.Interfacing.KH
{
    class Permissions
    {
        public static string 學期結算_For高雄 { get { return "K12.Club.Interfacing.ForKaoHsiung.1"; } }
        public static bool 學期結算_For高雄權限
        {
            get
            {
                return FISCA.Permission.UserAcl.Current[學期結算_For高雄].Executable;
            }
        }

        public static string 社團記錄_For高雄 { get { return "K12.Club.Interfacing.ForKaoHsiung.2"; } }
        public static bool 社團記錄For高雄權限
        {
            get
            {
                return FISCA.Permission.UserAcl.Current[社團記錄_For高雄].Executable;
            }
        }
    }
}
