using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class RoleInfo
    {
        public int id;
        public string name;
        public string description;
        public int x;
        public int y;

        public RoleInfo(Role role, CultureInfo culture)
        {
            id = (int)role;
            name = role.ToLocalizedString(culture);
            description = role.GetLocalizedDescription(culture);
            switch (role)
            {
                case Role.None:
                    x = 0;
                    y = 0;
                    break;
                case Role.Citizen:
                    x = 12;
                    y = 1;
                    break;
                case Role.FortuneTeller:
                    x = 9;
                    y = 0;
                    break;
                case Role.Shaman:
                    x = 11;
                    y = 7;
                    break;
                case Role.Hunter:
                    x = 10;
                    y = 0;
                    break;
                case Role.Cat:
                    x = 8;
                    y = 5;
                    break;
                case Role.Lover:
                    x = 1;
                    y = 2;
                    break;
                case Role.Poacher:
                    x = 11;
                    y = 0;
                    break;

                case Role.Werewolf:
                    x = 11;
                    y = 12;
                    break;
                case Role.Psycho:
                    x = 8;
                    y = 12;
                    break;
                case Role.Fanatic:
                    x = 9;
                    y = 12;
                    break;
                case Role.ElderWolf:
                    x = 10;
                    y = 12;
                    break;

                case Role.Fox:
                    x = 7;
                    y = 10;
                    break;
                case Role.ShintoPriest:
                    x = 1;
                    y = 10;
                    break;
            }
        }
    }
}
