using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace K12.Club.General.Interfacing.KH
{
    class Clearing : Campus.IRewrite.Interface.IClubClearingFormAPI
    {
        public FISCA.Presentation.Controls.BaseForm CreateBasicForm()
        {
            ClearingForm c = new ClearingForm();
            return c;
        }
    }
}
