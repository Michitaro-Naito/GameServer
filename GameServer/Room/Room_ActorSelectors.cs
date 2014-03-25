using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public partial class Room
    {
        // ----- Basic -----
        public IEnumerable<Actor> AliveActors
        {
            get { return _actors.Where(a => !a.IsDead); }
        }
        /*public List<Actor> AliveActors(Func<Actor, bool> predicate)
        {
            return AliveActors.Where(predicate).ToList();
        }*/
        public void ForEachAliveActors(Func<Actor, bool> predicate, Action<Actor> action)
        {
            AliveActors.Where(predicate).ToList().ForEach(action);
        }

        // ----- Team -----
        public IEnumerable<Actor> AliveCitizenTeam
        {
            get { return AliveActors.Where(a=>a.role.Is(Faction.Citizen)); }
        }
        public IEnumerable<Actor> AliveWerewolfTeam
        {
            get { return AliveActors.Where(a => a.role.Is(Faction.Werewolf)); }
        }
        public IEnumerable<Actor> AliveFoxTeam
        {
            get { return AliveActors.Where(a => a.role.Is(Faction.Fox)); }
        }

        // ----- Race -----
        public IEnumerable<Actor> AliveHumanRace
        {
            get { return AliveActors.Where(a => a.role.Is(Race.Human)); }
        }
        public IEnumerable<Actor> AliveWerewolfRace
        {
            get { return AliveActors.Where(a => a.role.Is(Race.Werewolf)); }
        }
        public IEnumerable<Actor> AliveFoxRace
        {
            get { return AliveActors.Where(a => a.role.Is(Race.Fox)); }
        }

        // ----- Role -----
        public IEnumerable<Actor> AliveFortuneTellers
        {
            get { return AliveActors.Where(a => a.role == Role.FortuneTeller); }
        }
    }
}
