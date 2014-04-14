using MyResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public partial class Room
    {
        void CountDownToStart()
        {
            duration = 5;
            Sync();
        }

        void Start()
        {
            var min = 7;
            var count = Math.Max(min, _characters.Count);

            // Adds Actors
            while (_actors.Count < count)
                _actors.Add(Actor.CreateUnique(_actors));

            // Remove NPCs
            while (_actors.Where(a => a.IsNPC).Count() > 0 && _actors.Count > min)
            {
                var npcToRemove = _actors.Where(a => a.character == null).RandomElement();
                _actors.Remove(npcToRemove);
            }

            // Casts Roles
            var dic = RoleHelper.CastRolesAuto(count);
            foreach (var p in dic)
            {
                for (var n = 0; n < p.Value; n++)
                    _actors.Where(a => a.role == Role.None).RandomElement().role = p.Key;
            }

            // Changes State
            RoomState = RoomState.Playing;
            duration = conf.interval;
            day = 1;

            NotifyDayDawns();

            // Tells FortuneTellers who is the true friend.
            ForEachAliveActors(a => a.CanFortuneTell, a =>
            {
                var friend = AliveActors.Where(f => f != a && f.Faction == Faction.Citizen).RandomElement();
                if (friend != null)
                    SystemMessageTo(a, new InterText("AIsTrueFriendOfCitizens", _.ResourceManager, new[] { friend.TitleAndName }));
            });

            _needSync = true;
        }

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
            _needSync = true;
        }

        bool _Z_CheckForVictory()
        {
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
                FactionWon = factionWon.Value;
                SystemMessageAll(new InterText("FactionAWon", _.ResourceManager, new[] { factionWon.Value.ToInterText() }));
                RoomState = RoomState.Ending;
                duration = 2 * conf.interval;
                // Opens Messages
                _actors.ForEach(a => SendFirstMessagesTo(a));
                // Quit GotoNextDay process.
                return true;
            }

            return false;
        }

        bool _Z_NpcVote()
        {
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
            AliveFortuneTellers.ToList().ForEach(a =>
            {
                var target = a.ActorToFortuneTell;
                if (target == null)
                {
                    target = _actors.RandomElement();
                }
                SystemMessageTo(a, new InterText("ASenseThatBIsC", _.ResourceManager, new []{ a.TitleAndName, target.TitleAndName, target.Race.ToInterText() }));
            });
            return false;
        }

        bool _Z_Execute()
        {
            if (AliveActors.Count() < 2)
            {
                SystemMessageAll(new InterText("NotEnoughActorsToVote", _.ResourceManager));
                return false;
            }
            var str = new List<InterText>();
            str.Add(new InterText("CitizenActions", _.ResourceManager));
            str.Add(new InterText("--------------------", null));
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

                str.Add(new InterText("{0} {1} => {2} {3}", null, new []{a.title, a.name, target.title, target.name}));
            });
            str.Add(new InterText("--------------------", null));
            foreach (KeyValuePair<Actor, int> p in dic)
            {
                str.Add(new InterText("{0} {1} : {2}", null, new []{p.Key.title, p.Key.name, new InterText(p.Value.ToString(), null)}));
            }
            str.Add(new InterText("--------------------", null));

            var max = dic.Max(p => p.Value);
            if (max <= 1)
            {
                str.Add(new InterText("NobodyExecutedBecauseOfInsufficientNumberOfVotes", _.ResourceManager));
                SystemMessageAll(str.ToArray());
            }
            else
            {
                var actorToExecute = dic.Where(p => p.Value == max).RandomElement().Key;
                actorToExecute.IsDead = true;
                str.Add(new InterText("ABIsExecuted", _.ResourceManager, new[] { actorToExecute.title, actorToExecute.name }));
                SystemMessageAll(str.ToArray());

                // Tells Shaman who has been killed.
                ForEachAliveActors(a => a.CanKnowDead, a =>
                {
                    SystemMessageTo(a, new InterText("ItHasBeenProvedThatKilledAIsB", _.ResourceManager, new []{ actorToExecute.TitleAndName, actorToExecute.Race.ToInterText() }));
                });
            }

            return false;
        }

        /// <summary>
        /// Handles Attacks of Werewolves.
        /// </summary>
        /// <returns></returns>
        bool _Z_Attack()
        {
            if (AliveActors.Count() < 2)
            {
                SystemMessageAll(new InterText("NotEnoughActorsToVote", _.ResourceManager));
                return false;
            }

            if (AliveWerewolfRace.Count() == 0)
            {
                SystemMessageAll(new InterText("ThereIsNoWerewolf", _.ResourceManager));
                return false;
            }

            // Werewolves' vote
            var str = new List<InterText>();
            str.Add(new InterText("AttackOfWerewolves", _.ResourceManager));
            str.Add(new InterText("--------------------", null));
            var dic = new Dictionary<Actor, int>();
            AliveWerewolfRace.ToList().ForEach(w =>
            {
                var target = w.ActorToExecute;
                var strRandom = new InterText("", null);
                if (target == null || target.IsDead)
                {
                    target = AliveActors.RandomElement();
                    strRandom = new InterText("Random", _.ResourceManager);
                }
                if (!dic.ContainsKey(target))
                    dic[target] = 0;
                dic[target]++;

                str.Add(new InterText("{0} {1} => {2} {3} {4}", null, new[] { w.title, w.name, target.title, target.name, strRandom }));
            });
            str.Add(new InterText("--------------------", null));
            foreach (KeyValuePair<Actor, int> p in dic)
                str.Add(new InterText("{0} {1} : {2}", null, new[] { p.Key.title, p.Key.name, new InterText(p.Value.ToString(), null) }));
            str.Add(new InterText("--------------------", null));
            var max = dic.Max(p => p.Value);
            var actorToAttack = dic.Where(p => p.Value == max).RandomElement().Key;
            str.Add(new InterText("AttackingAB", _.ResourceManager, new[] { actorToAttack.title, actorToAttack.name }));
            SystemMessageAll(str.ToArray());

            // Guards
            var actorsGuarded = new List<Actor>();
            AliveActors.Where(a => a.CanGuard).ToList().ForEach(h =>
            {
                Actor actorToGuard;
                if (h.ActorToGuard != null && !h.ActorToGuard.IsDead)
                    actorToGuard = h.ActorToGuard;
                else
                    actorToGuard = AliveActors.Where(a => a != h).RandomElement();
                actorsGuarded.Add(actorToGuard);
                SystemMessageTo(h, new InterText("AIsGuardingB", _.ResourceManager, new []{ h.TitleAndName, actorToGuard.TitleAndName }));
            });

            // Killed?
            if (actorsGuarded.Contains(actorToAttack))
            {
                // Saved by Hunter.
                SystemMessageAll(new InterText("NobodyKilledBecauseOfHuntersActivity", _.ResourceManager));
            }
            else
            {
                // Killed
                actorToAttack.IsDead = true;
                SystemMessageAll(new InterText("AHasBeenKilledByWerewolves", _.ResourceManager, new[] { actorToAttack.TitleAndName }));

                // Tells Shaman who has been killed.
                ForEachAliveActors(a => a.CanKnowDead, a =>
                {
                    SystemMessageTo(a, new InterText("ItHasBeenProvedThatKilledAIsB", _.ResourceManager, new[] { actorToAttack.TitleAndName, actorToAttack.Race.ToInterText() }));
                });
            }

            return false;
        }

        bool _Z_IncrementDay()
        {
            // Increment Day
            duration = conf.interval;
            day++;

            NotifyDayDawns();

            return false;
        }

        void NotifyDayDawns()
        {
            var str = new List<InterText>();
            str.Add(new InterText("--------------------", null));
            str.Add(new InterText("DayADawns", _.ResourceManager, new[] { new InterText(day.ToString(), null) }));
            str.Add(new InterText("--------------------", null));
            if (day == 1)
            {
                // First day message
                str.Add(new InterText("FirstVictimFound", _.ResourceManager));
                var dic = new Dictionary<Role, int>();
                _actors.ForEach(a =>
                {
                    if (!dic.ContainsKey(a.role))
                        dic[a.role] = 0;
                    dic[a.role]++;
                });
                var orderedDictionary = dic.OrderBy(en => en.Key);
                foreach (var entry in orderedDictionary)
                {
                    str.Add(new InterText("{0}: {1}", null, new []{ new InterText(entry.Key.ToKey(), _Enum.ResourceManager), new InterText(entry.Value.ToString(), null) }));
                }

                SendRules();
            }
            SystemMessageAll(str.ToArray());
        }
    }
}
