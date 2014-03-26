using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class GenderInfo
    {
        public int id;
        public string name;
        public string description;

        public GenderInfo(Gender gender, CultureInfo culture)
        {
            id = (int)gender;
            name = gender.ToLocalizedString(culture);
            description = gender.GetLocalizedDescription(culture);
        }
    }
}
