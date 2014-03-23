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

    public class ActorInfo
    {
        public int id;
        public string title;
        public string name;
        public Gender gender;
        public Role role;
        public string character;
        public bool isDead;

        public ActorInfo(Player player, Actor actor)
        {
            id = actor.id;
            title = actor.title.GetStringFor(player);
            name = actor.name.GetStringFor(player);
            gender = actor.gender;
            role = actor.role;
            if (actor.character != null)
                character = actor.character.ToString();
            isDead = actor.IsDead;
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

        Actor()
        {
            /*title = new InterText(MyResources._Title.ResourceManager.RandomKey(), InterText.InterTextType.Title);
            gender = new Gender[] { Gender.Male, Gender.Female }.RandomElement();
            if (gender == Gender.Male)
                name = new InterText(MyResources._MaleName.ResourceManager.RandomKey(), InterText.InterTextType.MaleName);
            else
                name = new InterText(MyResources._FemaleName.ResourceManager.RandomKey(), InterText.InterTextType.FemaleName);*/
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
