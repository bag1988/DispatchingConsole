using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Utilities
{
    public static class SubSystemName
    {
        public static string Get(int? subSystemID)
        {
            return subSystemID switch
            {
                SubsystemType.SUBSYST_ASO => "АСО",
                SubsystemType.SUBSYST_SZS => "УУЗС",
                SubsystemType.SUBSYST_GSO_STAFF => "ПУ",
                SubsystemType.SUBSYST_P16x => "П16х",
                _ => "НЕТ ТАКОЙ ПОДСИСТЕМЫ"
            };
        }
    }
}
