using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public partial class Room
    {
        void GotoNextDay()
        {
            new List<Func<bool>>()
            {
                _Z_NpcVote,
                _Z_FortuneTell,
                _Z_CheckForVictory,
                _Z_Execute,
                _Z_CheckForVictory,
                _Z_Attack,
                _Z_CheckForVictory,
                _Z_IncrementDay
            }.Do();
        }

        bool _Z_CheckForVictory()
        {
            //SystemMessageAll("Checking for victory...");

            var factionWon = new Nullable<Faction>();

            var citizens = AliveHumanRace.Count();
            var wolves = AliveWerewolfRace.Count();
            var foxes = AliveFoxRace.Count();

            // Important: Alive citizens include FOX
            citizens += foxes;

            // Citizens won?
            if (citizens > 0 && wolves == 0)
            {
                if (foxes > 0)
                    factionWon = Faction.Fox;
                else
                    factionWon = Faction.Citizen;
            }

            // Werewolves won?
            if (citizens <= wolves)
            {
                if (foxes > 0)
                    factionWon = Faction.Fox;
                else
                    factionWon = Faction.Werewolf;
            }

            // Withdraw?
            if (AliveActors.Count() == 0)
                factionWon = Faction.None;

            if (factionWon != null)
            {
                SystemMessageAll("Faction won: " + factionWon.Value);
                RoomState = RoomState.Ending;
                // Quit GotoNextDay process.
                return true;
            }

            return false;
        }

        bool _Z_NpcVote()
        {
            //SystemMessageAll("NPC voting...");
            _actors.Where(a => a.character == null).ToList().ForEach(a =>
            {
                a.ActorToExecute = _actors.RandomElement();
                a.ActorToAttack = _actors.RandomElement();
                a.ActorToFortuneTell = _actors.RandomElement();
                a.ActorToGuard = _actors.RandomElement();
            });
            return false;
        }

        bool _Z_FortuneTell()
        {
            //SystemMessageAll("FortuneTelling...");
            AliveFortuneTellers.ToList().ForEach(a =>
            {
                var target = a.ActorToFortuneTell;
                var random = false;
                if (target == null)
                {
                    target = _actors.RandomElement();
                    random = true;
                }
                SystemMessageAll(string.Format("{0} fortunetelled {1}. Result: {2} Random:{3}", a, target, target.role, random));
            });
            return false;
        }

        bool _Z_Execute()
        {
            //SystemMessageAll("Executing...");
            if (AliveActors.Count() < 2)
            {
                SystemMessageAll("Not enough Actors to vote.");
                return false;
            }
            var dic = new Dictionary<Actor, int>();
            AliveActors.ToList().ForEach(a =>
            {
                var target = a.ActorToExecute;
                var random = false;
                if (target == null || target.IsDead)
                {
                    target = AliveActors.RandomElement();
                    random = true;
                }
                if (!dic.ContainsKey(target))
                    dic[target] = 0;
                dic[target]++;
            });
            /*foreach (KeyValuePair<Actor, int> p in dic)
            {
                SystemMessageAll(string.Format("{0}:{1}", p.Key, p.Value));
            }*/
            var max = dic.Max(p => p.Value);
            if (max <= 1)
            {
                SystemMessageAll("VoteCount <= 1. Nobody executed.");
                return false;
            }
            var actorToExecute = dic.Where(p => p.Value == max).RandomElement().Key;

            actorToExecute.IsDead = true;
            SystemMessageAll(string.Format("Executed:{0}", actorToExecute));

            return false;
        }

        /// <summary>
        /// Handles Attacks of Werewolves.
        /// </summary>
        /// <returns></returns>
        bool _Z_Attack()
        {
            //SystemMessageAll("Attacking...");
            if (AliveActors.Count() < 2)
            {
                SystemMessageAll("Not enough Actors to eat.");
                return false;
            }
            if (AliveWerewolfRace.Count() == 0)
            {
                SystemMessageAll("There is no werewolf.");
                return false;
            }
            var dic = new Dictionary<Actor, int>();
            AliveWerewolfRace.ToList().ForEach(w =>
            {
                var target = w.ActorToExecute;
                var random = false;
                if (target == null || target.IsDead)
                {
                    target = AliveActors.RandomElement();
                    random = true;
                }
                if (!dic.ContainsKey(target))
                    dic[target] = 0;
                dic[target]++;
            });
            /*foreach (KeyValuePair<Actor, int> p in dic)
            {
                SystemMessageAll(string.Format("{0}:{1}", p.Key, p.Value));
            }*/
            var max = dic.Max(p => p.Value);
            var actorToAttack = dic.Where(p => p.Value == max).RandomElement().Key;

            actorToAttack.IsDead = true;
            SystemMessageAll(string.Format("Killed:{0}", actorToAttack));

            return false;
        }

        bool _Z_IncrementDay()
        {
            // Increment Day
            duration = interval;
            day++;

            SystemMessageAll(string.Format("Day {0} dawns.", day));

            return false;
        }
    }
}
