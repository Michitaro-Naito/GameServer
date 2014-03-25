using MyResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public enum Faction : ushort
    {
        None            = 0000,

        Citizen         = 0001,
        Werewolf        = 0002,
        Fox             = 0003
    }

    public enum Race : ushort
    {
        Human           = 0001,
        Werewolf        = 0002,
        Fox             = 0003
    }

    public enum Role : ushort
    {
        None            = 0000,

        Citizen         = 1000,
        FortuneTeller   = 1001,
        Shaman          = 1002,
        Hunter          = 1003,

        Werewolf        = 2000,
        Psycho          = 2001,

        Fox             = 3000
    }

    public static class RoleExtension
    {
        public static bool Is(this Role role, Faction faction)
        {
            return faction == role.GetFaction();
        }
        public static bool Is(this Role role, Race race)
        {
            return race == role.GetRace();
        }

        public static Faction GetFaction(this Role role)
        {
            switch (role)
            {
                case Role.Citizen:
                case Role.FortuneTeller:
                case Role.Shaman:
                case Role.Hunter:
                default:
                    return Faction.Citizen;

                case Role.Werewolf:
                case Role.Psycho:
                    return Faction.Werewolf;

                case Role.Fox:
                    return Faction.Fox;
            }
        }
        public static Race GetRace(this Role role)
        {
            switch (role)
            {
                case Role.Citizen:
                case Role.FortuneTeller:
                case Role.Shaman:
                case Role.Hunter:
                case Role.Psycho:
                default:
                    return Race.Human;

                case Role.Werewolf:
                    return Race.Werewolf;

                case Role.Fox:
                    return Race.Fox;
            }
        }
        public static InterText ToInterText(this Faction faction)
        {
            return new InterText("[{0}]", null, new[] { new InterText(faction.ToKey(), _Enum.ResourceManager) });
        }
        public static InterText ToInterText(this Race race)
        {
            return new InterText("[{0}]", null, new[] { new InterText(race.ToKey(), _Enum.ResourceManager) });
        }
    }

    public static class RoleHelper
    {
        public static Dictionary<Role, int> GetRoleCountsDictionary()
        {
            var dic = new Dictionary<Role, int>();
            foreach (var role in Enum.GetValues(typeof(Role)).Cast<Role>())
                dic[role] = 0;
            //if (removeNone) dic.Remove(Role.None);

            return dic;
        }

        public static Dictionary<Role, int> CastRolesAuto(int totalActors)
        {
            var dic = GetRoleCountsDictionary();

            dic[Role.FortuneTeller] = Math.Max(1, (totalActors / 9.0).RandomRound());
            dic[Role.Shaman] = Math.Max(1, (totalActors / 9.0).RandomRound());
            dic[Role.Hunter] = (totalActors / 9.0).RandomRound();
            dic[Role.Psycho] = (totalActors / 9.0).RandomRound();

            // No Fox
            dic[Role.Werewolf] = Math.Max(1, (int)Math.Floor(totalActors / 3.5));

            dic[Role.Citizen] = totalActors - dic.Sum(pair => pair.Value);

            return dic;
        }
    }
}
