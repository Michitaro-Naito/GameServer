using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public Role role;
        public string character;
        public bool isDead;
        public bool isRoomMaster;

        public ActorInfo(Room room, Player player, Actor actor)
        {
            id = actor.id;
            title = actor.title.GetStringFor(player);
            name = actor.name.GetStringFor(player);
            gender = actor.gender;
            role = actor.role;
            if (actor.character != null)
                character = actor.character.ToString();
            isDead = actor.IsDead;
            isRoomMaster = room.IsRoomMaster(actor);
        }
    }

    public class Actor
    {
        public int id;
        public InterText title;
        public InterText name;
        public Gender gender;
        public Role role;
        public Character character;

        public Actor ActorToExecute;
        public Actor ActorToAttack;
        public Actor ActorToFortuneTell;
        public Actor ActorToGuard;

        public bool IsDead { get; set; }
        public bool IsNPC { get { return character == null; } }

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

        public static List<Actor> Create(int amount){
            if(amount <= 0)
                throw new ArgumentException("amount must be > 0");
            var actors = new List<Actor>();
            var titleKeys = MyResources._Title.ResourceManager.Keys();
            if (titleKeys.Count < amount)
                throw new ArgumentException("Not enough titleKeys");
            var maleNameKeys = MyResources._MaleName.ResourceManager.Keys();
            if (maleNameKeys.Count < amount)
                throw new ArgumentException("Not enough maleNameKeys");
            var femaleNameKeys = MyResources._FemaleName.ResourceManager.Keys();
            if (femaleNameKeys.Count < amount)
                throw new ArgumentException("Not enough femaleNameKeys");

            for (var n = 0; n < amount; n++)
            {
                var actor = new Actor();

                // id = index;
                actor.id = n;

                // Random Title
                var titleKey = titleKeys.RandomElement();
                titleKeys.Remove(titleKey);
                actor.title = new InterText(titleKey, InterText.InterTextType.Title);

                // Random Gender
                actor.gender = new Gender[] { Gender.Male, Gender.Female }.RandomElement();

                // Random Name
                InterText name = null;
                if (actor.gender == Gender.Male)
                {
                    var maleNameKey = maleNameKeys.RandomElement();
                    maleNameKeys.Remove(maleNameKey);
                    name = new InterText(maleNameKey, InterText.InterTextType.MaleName);
                }
                else
                {
                    var femaleNameKey = femaleNameKeys.RandomElement();
                    femaleNameKeys.Remove(femaleNameKey);
                    name = new InterText(femaleNameKey, InterText.InterTextType.FemaleName);
                }
                actor.name = name;

                actors.Add(actor);
            }
            var dic = new Dictionary<string, int>();
            return actors;
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
            actor.title = new InterText(titleKey, InterText.InterTextType.Title);

            // Random Name
            InterText name = null;
            if (actor.gender == Gender.Male)
            {
                var maleNameKey = maleNameKeys.RandomElement();
                name = new InterText(maleNameKey, InterText.InterTextType.MaleName);
            }
            else
            {
                var femaleNameKey = femaleNameKeys.RandomElement();
                name = new InterText(femaleNameKey, InterText.InterTextType.FemaleName);
            }
            actor.name = name;

            return actor;
        }

        Actor()
        {
        }

        public override string ToString()
        {
            var alive = IsDead ? "Dead" : "Alive";
            return string.Format(
                "[{0}{1}({2}) {3} {4}]",
                title, name, character,
                role, alive);
            //return string.Format("[Actor title:{0} name:{1} role:{2} IsDead:{3} character:{4}]", title, name, role, IsDead, character);
        }
    }
}
