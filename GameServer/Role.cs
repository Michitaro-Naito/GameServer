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
        Cat             = 1004,//
        Lover           = 1005,//
        Poacher         = 1006,//

        Werewolf        = 2000,
        Psycho          = 2001,
        Fanatic         = 2002,//
        ElderWolf       = 2003,//

        Fox             = 3000,
        ShintoPriest    = 3001,//
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
                case Role.Cat:
                case Role.Lover:
                case Role.Poacher:
                default:
                    return Faction.Citizen;

                case Role.Werewolf:
                case Role.Psycho:
                case Role.Fanatic:
                case Role.ElderWolf:
                    return Faction.Werewolf;

                case Role.Fox:
                case Role.ShintoPriest:
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
                case Role.Fanatic:
                case Role.Cat:
                case Role.Lover:
                case Role.ElderWolf:
                case Role.ShintoPriest:
                default:
                    return Race.Human;

                case Role.Werewolf:
                case Role.Poacher:
                    return Race.Werewolf;

                case Role.Fox:
                    return Race.Fox;
            }
        }
        public static bool CountAs(this Role role, Race race) {
            switch (race) {
                case Race.Human:
                    switch (role) {
                        case Role.Werewolf:
                        case Role.ElderWolf:
                            // All except Werewolves count as Human.
                            return false;
                    }
                    return true;

                case Race.Werewolf:
                    switch (role) {
                        case Role.Werewolf:
                        case Role.ElderWolf:
                            // Only Werewolves count as Werewolves.
                            return true;
                    }
                    return false;

                case Race.Fox:
                    switch (role) {
                        case Role.Fox:
                            // Only Fox count as Fox.
                            return true;
                    }
                    return false;
            }
            return false;
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
            //dic[Role.Werewolf] = Math.Max(1, (int)Math.Floor(totalActors / 3.5));
            dic[Role.Werewolf] = Math.Max(1, (int)Math.Floor(totalActors / 4.0));
            dic[Role.Fox] = Math.Max(1, (int)Math.Floor(totalActors / 12.0));

            dic[Role.Citizen] = totalActors - dic.Sum(pair => pair.Value);

            return dic;
        }

        internal static Dictionary<Role, int> CastRolesManual(List<ClientModel.ClientRoleAmount> roles, int totalActors) {
            var dic = GetRoleCountsDictionary();

            roles.ForEach(role => dic[role.id] = role.amount);

            if (dic.Any(en => en.Value < 0))
                throw new ClientException(InterText.Create("CannotBeMinus", _Error.ResourceManager));

            var sum = dic.Sum(en => en.Value);
            if (sum != totalActors)
                throw new ClientException(InterText.Create("SumMustBeA", _Error.ResourceManager, totalActors));

            if (dic[Role.None] > 0)
                throw new ClientException(InterText.Create("AMustBeB", _Error.ResourceManager, new InterText(Role.None.ToKey(), _Enum.ResourceManager), 0));

            if (dic.Sum(en => en.Key.Is(Faction.Citizen) ? en.Value : 0) == 0)
                throw new ClientException(InterText.Create("AMustBeBiggerThanB", _Error.ResourceManager, new InterText(Faction.Citizen.ToKey(), _Enum.ResourceManager), 0));

            var werewolfCount = dic.Sum(en => en.Key.CountAs(Race.Werewolf) ? en.Value : 0);// dic[Role.Werewolf];
            if (werewolfCount == 0)
                throw new ClientException(InterText.Create("AMustBeBiggerThanB", _Error.ResourceManager, new InterText(Race.Werewolf.ToKey(), _Enum.ResourceManager), 0));
                //throw new Exception("人狼を1人以上加えてください。");
            if (werewolfCount >= dic.Sum(en => en.Value) / 2.0)
                //throw new Exception("人狼はプレイヤー数の半分未満にしてください。");
                throw new ClientException(InterText.Create("WerewolvesMustBeSmallerThanHalfOfTotal", _Error.ResourceManager));

            return dic;
        }
    }
}
