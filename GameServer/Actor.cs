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
    public class Actor
    {
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

        public Actor()
        {
            title = new InterText(MyResources._Title.ResourceManager.RandomKey(), InterText.InterTextType.Title);
            gender = new Gender[] { Gender.Male, Gender.Female }.RandomElement();
            if (gender == Gender.Male)
                name = new InterText(MyResources._MaleName.ResourceManager.RandomKey(), InterText.InterTextType.MaleName);
            else
                name = new InterText(MyResources._FemaleName.ResourceManager.RandomKey(), InterText.InterTextType.FemaleName);
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
