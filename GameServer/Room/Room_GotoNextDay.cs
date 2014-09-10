using GameServer.ClientModel;
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
            _needSync = true;
        }

        /// <summary>
        /// Casts roles.
        /// </summary>
        /// <param name="roles"></param>
        bool CastRoles(List<ClientRoleAmount> roles) {
            var min = 7;
            //var count = Math.Max(min, _characters.Count);
            var count = Math.Max(min, AliveActors.Count());

            // Adds Actors
            //while (_actors.Count < count)
            while (AliveActors.Count() < count)
                _actors.Add(Actor.CreateUnique(_actors, conf.characterNameSet));

            // Remove NPCs
            //while (_actors.Where(a => a.IsNPC).Count() > 0 && _actors.Count > min) {
            while (AliveActors.Where(a => a.IsNPC).Count() > 0 && AliveActors.Count() > min) {
                var npcToRemove = AliveActors.Where(a => a.character == null).RandomElement();
                _actors.Remove(npcToRemove);
            }

            var aliveNoRoleCharacters = AliveActors.Count(a => a.role == Role.None);
            // Casts Roles
            var dic = RoleHelper.CastRolesAuto(aliveNoRoleCharacters);
            if (roles != null) {
                // Manual
                try {
                    dic = RoleHelper.CastRolesManual(roles, aliveNoRoleCharacters);
                }
                catch (ClientException e) {
                    SystemMessageAll(e.Errors.ToArray());
                    SystemMessageAll("配役にエラーがあるため開始できませんでした。");
                    return false;
                }
            }
            foreach (var p in dic) {
                for (var n = 0; n < p.Value; n++) {
                    var actorToSet = AliveActors.Where(a => a.role == Role.None).RandomElement();
                    if(actorToSet!=null)
                        actorToSet.role = p.Key;
                }
                    //_actors.Where(a => a.role == Role.None).RandomElement().role = p.Key;
            }

            return true;
        }

        void Start() {
            //CastRoles();

            // Changes State
            RoomState = RoomState.Playing;
            duration = conf.interval;
            day = 1;

            NotifyDayDawns();

            if (!conf.noFirstDayFortuneTelling) {
                // Tells FortuneTellers who is the true friend.
                ForEachAliveActors(a => a.CanFortuneTell, a => {
                    var friend = AliveActors.Where(f => f != a && f.Faction == Faction.Citizen).RandomElement();
                    if (friend != null)
                        SystemMessageTo(a, new InterText("AIsTrueFriendOfCitizens", _.ResourceManager, new[] { friend.TitleAndName }));
                });
            }

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
                _Z_SuicideTime,
                _Z_CheckForVictory,
                _Z_IncrementDay
            }.Do();
            _needSync = true;
        }

        bool _Z_CheckForVictory()
        {
            var factionWon = new Nullable<Faction>();

            var citizens = AliveActors.Where(a => a.role.CountAs(Race.Human)).Count();
            var wolves = AliveActors.Where(a=>a.role.CountAs(Race.Werewolf)).Count();
            var foxes = AliveActors.Where(a=>a.role.CountAs(Race.Fox)).Count();
            //SystemMessageAll(string.Format("Alive: {0}, {1}, {2}", citizens, wolves, foxes));

            // Important: Alive citizens include FOX
            //citizens += foxes;

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
                // Opens Messages for Players and Spectators
                //_actors.ForEach(a => SendFirstMessagesTo(a));
                CharactersAndSpectators.ToList().ForEach(c => SendFirstMessagesTo(c));
                // Quit GotoNextDay process.
                return true;
            }

            return false;
        }

        bool _Z_NpcVote()
        {
            /*_actors.Where(a => a.character == null).ToList().ForEach(a =>
            {
                a.ActorToExecute = _actors.Where(t=>t!=a).RandomElement();
                a.ActorToAttack = _actors.Where(t => t != a).RandomElement();
                a.ActorToFortuneTell = _actors.Where(t => t != a).RandomElement();
                a.ActorToGuard = _actors.Where(t => t != a).RandomElement();
            });*/
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
                if (target.Race == Race.Fox) {
                    // Target is Fox. Killed.
                    target.IsDead = true;
                    SystemMessageAll(InterText.Create("AHasBeenKilledByFortuneTelling", _.ResourceManager, target.TitleAndName));
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
                var strRandom = new InterText("", null);
                if (target == null || target.IsDead)
                {
                    // Invalid target. Select one except mates.
                    if (a.CanShareWerewolfCommunity)
                        target = AliveActors.Where(t => !t.CanShareWerewolfCommunity).RandomElement();
                    else if (a.CanShareFoxCommunity)
                        target = AliveActors.Where(t => !t.CanShareFoxCommunity).RandomElement();
                    else if (a.CanShareLoverCommunity)
                        target = AliveActors.Where(t => !t.CanShareLoverCommunity).RandomElement();
                    else
                        target = AliveActors.Where(t => t != a).RandomElement();

                    if (target == null)
                        // Still invalid. Select one randomly.
                        target = AliveActors.RandomElement();

                    strRandom = new InterText("Random", _.ResourceManager);
                }
                if (target != null) {
                    if (!dic.ContainsKey(target))
                        dic[target] = 0;
                    dic[target]++;

                    str.Add(new InterText("{0} {1} => {2} {3} {4}", null, new[] { a.title, a.name, target.title, target.name, strRandom }));
                }
            });
            if (dic.Count == 0)
                // No votes.
                return false;

            str.Add(new InterText("--------------------", null));
            foreach (KeyValuePair<Actor, int> p in dic)
            {
                str.Add(new InterText("{0} {1} : {2}", null, new []{p.Key.title, p.Key.name, new InterText(p.Value.ToString(), null)}));
            }
            str.Add(new InterText("--------------------", null));

            var max = dic.Max(p => p.Value);
            if (max <= 1)
            {
                // Nobody Executed
                str.Add(new InterText("NobodyExecutedBecauseOfInsufficientNumberOfVotes", _.ResourceManager));
                SystemMessageAll(str.ToArray());
            }
            else
            {
                // Executed
                var actorToExecute = dic.Where(p => p.Value == max).RandomElement().Key;
                actorToExecute.IsDead = true;
                str.Add(new InterText("ABIsExecuted", _.ResourceManager, new[] { actorToExecute.title, actorToExecute.name }));
                SystemMessageAll(str.ToArray());

                if (actorToExecute.CanRevenge) {
                    // Cat's Revenge
                    var aliveCitizen = AliveActors.Where(a => a.Faction == Faction.Citizen).RandomElement();
                    if (aliveCitizen != null) {
                        aliveCitizen.IsDead = true;
                        SystemMessageAll(InterText.Create("AHasBeenKilledByCatsRevenge", _.ResourceManager, aliveCitizen.TitleAndName));
                    }
                }

                if (actorToExecute.role == Role.Lover) {
                    // Lover executed. Kill the other Lovers.
                    AliveActors.Where(a => a.role == Role.Lover).ToList().ForEach(a => {
                        a.IsDead = true;
                        SystemMessageAll(InterText.Create("AHasComittedSuicideBecauseOfDeathOfLover", _.ResourceManager, a.TitleAndName));
                    });
                }

                // Tells Shaman who has been killed.
                ForEachAliveActors(a => a.CanKnowDead, a => {
                    InterText victim = actorToExecute.Race.ToInterText();
                    if (conf.strongShaman)
                        victim = actorToExecute.role.ToInterText();
                    SystemMessageTo(a, new InterText("ItHasBeenProvedThatKilledAIsB", _.ResourceManager, new []{ actorToExecute.TitleAndName, victim }));
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

            if (AliveActors.Count(a=>a.role.CountAs(Race.Werewolf)) /*AliveWerewolfRace.Count()*/ == 0)
            {
                SystemMessageAll(new InterText("ThereIsNoWerewolf", _.ResourceManager));
                return false;
            }

            // Werewolves' vote
            var str = new List<InterText>();
            str.Add(new InterText("AttackOfWerewolves", _.ResourceManager));
            str.Add(new InterText("--------------------", null));
            var dic = new Dictionary<Actor, int>();
            AliveActors.Where(a=>a.role.CountAs(Race.Werewolf)).ToList().ForEach(w =>
            {
                var target = w.ActorToAttack;
                var strRandom = new InterText("", null);
                if (target == null || target.IsDead)
                {
                    // Invalid target. Select randomly except mates.
                    target = AliveActors.Where(t => !t.CanShareWerewolfCommunity).RandomElement();

                    if (target == null)
                        // Still invalid. Select one randomly.
                        target = AliveActors.RandomElement();

                    strRandom = new InterText("Random", _.ResourceManager);
                }
                if (target != null) {
                    if (!dic.ContainsKey(target))
                        dic[target] = 0;
                    dic[target]++;

                    str.Add(new InterText("{0} {1} => {2} {3} {4}", null, new[] { w.title, w.name, target.title, target.name, strRandom }));
                }
            });
            if (dic.Count == 0)
                // No votes. Skip attacking.
                return false;

            str.Add(new InterText("--------------------", null));
            foreach (KeyValuePair<Actor, int> p in dic)
                str.Add(new InterText("{0} {1} : {2}", null, new[] { p.Key.title, p.Key.name, new InterText(p.Value.ToString(), null) }));
            str.Add(new InterText("--------------------", null));
            var max = dic.Max(p => p.Value);
            var actorToAttack = dic.Where(p => p.Value == max).RandomElement().Key;
            str.Add(new InterText("AttackingAB", _.ResourceManager, new[] { actorToAttack.title, actorToAttack.name }));
            SystemMessageWolf(str.ToArray());

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
            else if (actorToAttack.Race == Race.Fox){
                // Evaded
                SystemMessageAll(new InterText("RunningFoxSpotted", _.ResourceManager));
            }
            else {
                // Killed
                actorToAttack.IsDead = true;
                SystemMessageAll(new InterText("AHasBeenKilledByWerewolves", _.ResourceManager, new[] { actorToAttack.TitleAndName }));

                if (actorToAttack.CanRevenge) {
                    // Cat's Revenge
                    var aliveWerewolf = AliveActors.Where(a => a.role.CountAs(Race.Werewolf)).RandomElement();
                    if (aliveWerewolf != null) {
                        aliveWerewolf.IsDead = true;
                        SystemMessageAll(InterText.Create("AHasBeenKilledByCatsRevenge", _.ResourceManager, aliveWerewolf.TitleAndName));
                    }
                }

                if (actorToAttack.role == Role.Lover) {
                    // Lover killed. Turn the other Lovers Hunters.
                    AliveActors.Where(a => a.role == Role.Lover).ToList().ForEach(a => {
                        a.role = Role.Hunter;
                        SystemMessageAll(InterText.Create("SomeoneHasBecomeHunterBecauseOfDeathOfLover", _.ResourceManager));
                        SystemMessageTo(a, InterText.Create("YourPartnerHasBeenKilledByWerewolvesBeHunter", _.ResourceManager));
                    });
                }

                // Tells Shaman who has been killed.
                ForEachAliveActors(a => a.CanKnowDead, a =>
                {
                    InterText victim = actorToAttack.Race.ToInterText();
                    if (conf.strongShaman)
                        victim = actorToAttack.role.ToInterText();
                    SystemMessageTo(a, new InterText("ItHasBeenProvedThatKilledAIsB", _.ResourceManager, new[] { actorToAttack.TitleAndName, victim }));
                });
            }

            return false;
        }

        bool _Z_SuicideTime() {
            if (AliveActors.Count(a => a.Race == Race.Fox) == 0) {
                AliveActors.Where(a => a.role == Role.ShintoPriest).ToList().ForEach(p => {
                    p.IsDead = true;
                    SystemMessageAll(InterText.Create("AHasCommitSuicideBecauseOfDeathOfFox", _.ResourceManager, p.TitleAndName));
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
