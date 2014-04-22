using MyResources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GameServer
{
    public enum Gender
    {
        None, Male, Female
    }

    public class VoteInfo
    {
        public Nullable<int> executeId, attackId, fortuneTellId, guardId;
    }

    public class ActorInfo
    {
        public int id;
        public string title;
        public string name;
        public Gender gender;
        public string character;
        public bool isDead;
        public bool isRoomMaster;
        public GameServer.ColorHelper.ColorIdentity ColorIdentity { get; set; }
        public bool isPresent;  // isNotAbsent, false if NPC

        public Role role;
        public bool isRoleSure;

        /*public ActorInfo(Room room, Player player, Actor viewer, Actor actor)
        {
            id = actor.id;
            title = actor.title.GetStringFor(player);
            name = actor.name.GetStringFor(player);
            gender = actor.gender;
            if (actor.character != null)
                character = actor.character.ToString();
            isDead = actor.IsDead;
            isRoomMaster = room.IsRoomMaster(actor);
            ColorIdentity = actor.ColorIdentity;
            isPresent = actor.IsNPC? false: room.HasCharacter(actor.character);

            if (new[] { RoomState.Matchmaking, RoomState.Playing }.Contains(room.RoomState))
            {
                // Filters
                role = Role.Citizen;
                isRoleSure = false;

                if ((actor == viewer)   // Alice can see herself.
                    || (new[] { Role.Werewolf }.Contains(viewer.role) && actor.role == Role.Werewolf))    // Werewolf or Fanatic can see werewolves.
                {
                    role = actor.role;
                    isRoleSure = true;
                }

                // Lover can see each other.
            }
            else
            {
                // Ending or Ended. Does not filter
                role = actor.role;
                isRoleSure = true;
            }
        }*/
    }

    public class Actor
    {
        public int id;
        public InterText title;
        public InterText name;
        public Gender gender;
        public Role role;
        public Character character;
        public GameServer.ColorHelper.ColorIdentity ColorIdentity { get; set; }

        public Actor ActorToExecute;
        public Actor ActorToAttack;
        public Actor ActorToFortuneTell;
        public Actor ActorToGuard;

        public bool IsDead { get; set; }
        public bool IsNPC { get { return character == null; } }
        public Faction Faction { get { return role.GetFaction(); } }
        public Race Race { get { return role.GetRace(); } }

        public bool CanFortuneTell { get { return role == Role.FortuneTeller; } }
        public bool CanKnowDead { get { return role == Role.Shaman; } }
        public bool CanGuard { get { return role == Role.Hunter || role == Role.Poacher; } }
        public bool CanShareWerewolfCommunity {
            get {
                switch (role) {
                    case Role.Werewolf:
                    case Role.ElderWolf:
                    case Role.Fanatic:
                        return true;
                }
                return false;
            }
        }
        public bool CanRevenge { get { return role == Role.Cat; } }
        public bool CanShareLoverCommunity { get { return role == Role.Lover; } }

        public InterText TitleAndName
        {
            get
            {
                return new InterText("[{0} {1}]", _.ResourceManager, new[] { title, name });
            }
        }

        public VoteInfo VoteInfo
        {
            get
            {
                var info = new VoteInfo();
                if (ActorToExecute != null)
                    info.executeId = ActorToExecute.id;
                if (ActorToAttack != null)
                    info.attackId = ActorToAttack.id;
                if (ActorToFortuneTell != null)
                    info.fortuneTellId = ActorToFortuneTell.id;
                if (ActorToGuard != null)
                    info.guardId = ActorToGuard.id;
                return info;
            }
        }

        public bool IsOwnedBy(Player player)
        {
            return character != null && character.Player == player;
        }

        public static Actor CreateUnique(List<Actor> existing)
        {
            // Remove existing keys
            var existingTitleKeys = existing.Select(a => a.title.Key);
            var titleKeys = MyResources._Title.ResourceManager.Keys();
            titleKeys.RemoveAll(key => existingTitleKeys.Contains(key));

            var existingMaleNameKeys = existing.Where(a=>a.gender==Gender.Male).Select(a => a.name.Key);
            var maleNameKeys = MyResources._MaleName.ResourceManager.Keys();
            maleNameKeys.RemoveAll(key => existingMaleNameKeys.Contains(key));

            var existingFemaleNameKeys = existing.Where(a => a.gender == Gender.Female).Select(a => a.name.Key);
            var femaleNameKeys = MyResources._FemaleName.ResourceManager.Keys();
            femaleNameKeys.RemoveAll(key => existingFemaleNameKeys.Contains(key));

            var actor = new Actor();

            // Index = max + 1
            if (existing.Count == 0)
                actor.id = 0;
            else
                actor.id = existing.Max(a => a.id) + 1;

            // Random Gender
            actor.gender = new Gender[] { Gender.Male, Gender.Female }.RandomElement();

            // Random Title
            var titleKey = titleKeys.RandomElement();
            actor.title = new InterText(titleKey, _Title.ResourceManager /*InterText.InterTextType.Title*/);

            // Random Name
            InterText name = null;
            if (actor.gender == Gender.Male)
            {
                var maleNameKey = maleNameKeys.RandomElement();
                name = new InterText(maleNameKey, _MaleName.ResourceManager);
            }
            else
            {
                var femaleNameKey = femaleNameKeys.RandomElement();
                name = new InterText(femaleNameKey, _FemaleName.ResourceManager);
            }
            actor.name = name;

            // Color
            actor.ColorIdentity = ColorHelper.GenerateColorIdentity(actor.id.ToString() + actor.title.Key + actor.name.Key);

            return actor;
        }

        Actor()
        {
        }

        public ActorInfo ToInfo(Room room, Player player, Actor viewer) {
            var info = new ActorInfo() {
                id = id,
                title = title.GetStringFor(player),
                name = name.GetStringFor(player),
                gender = gender,
                character = character != null ? character.ToString() : null,
                isDead = IsDead,
                isRoomMaster = room.IsRoomMaster(this),
                ColorIdentity = ColorIdentity,
                isPresent = IsNPC ? false : room.HasCharacter(character)
            };

            if (new[] { RoomState.Matchmaking, RoomState.Playing }.Contains(room.RoomState)) {
                // Filters
                info.role = Role.Citizen;
                info.isRoleSure = false;

                if (viewer != null
                    && ((this == viewer)   // Alice can see herself.
                    ||(viewer.CanShareWerewolfCommunity && this.CanShareWerewolfCommunity)  // Werewolf friends.
                    ||(viewer.CanShareLoverCommunity && this.CanShareLoverCommunity)    // Lovers.
                    //|| (new[] { Role.Werewolf, Role.Fanatic }.Contains(viewer.role) && new[]{Role.Werewolf, Role.Fanatic}.Contains(role))   // Werewolf or Fanatic can see werewolves.
                    || (room.IsRoomMaster(viewer) && viewer.IsDead)))    // Dead RoomMaster can see anything.
                {
                    info.role = role;
                    info.isRoleSure = true;
                }

                // Lover can see each other.
            }
            else {
                // Ending or Ended. Does not filter
                info.role = role;
                info.isRoleSure = true;
            }
            return info;
        }

        public override string ToString()
        {
            var alive = IsDead ? "Dead" : "Alive";
            return string.Format(
                "[{0}{1}({2}) {3} {4}]",
                title, name, character,
                role, alive);
        }

        public override bool Equals(object obj)
        {
            // Not null?
            if (obj == null)
                return false;

            // The same type?
            if (obj.GetType() != GetType())
                return false;

            var b = (Actor)obj;
            return this.id == b.id;
        }

        public static bool operator ==(Actor a, Actor b)
        {
            var oa = (object)a;
            var ob = (object)b;
            if (oa == null && ob == null)
                return true;
            if (oa == null || ob == null)
                return false;
            return a.id == b.id;
        }

        public static bool operator !=(Actor a, Actor b)
        {
            var oa = (object)a;
            var ob = (object)b;
            if (oa == null && ob == null)
                return false;
            if (oa == null || ob == null)
                return true;
            return a.id != b.id;
        }

        internal string ToHtml(System.Globalization.CultureInfo cultureInfo)
        {
            _UiString.Culture = cultureInfo;
            var internalString = string.Format("{0} {1}",
                TitleAndName.GetString(cultureInfo),
                role.ToLocalizedString(cultureInfo));
            if (character != null)
                internalString += string.Format(" by {0}", character.Name);
            else
                internalString += "(NPC)";
            var html = string.Format("<span style=\"color:{0};background-color:{1};\">{2}</span>",
                ColorIdentity.text,
                ColorIdentity.background,
                HttpUtility.HtmlEncode(internalString));
            return html;
        }
    }
}
