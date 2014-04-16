using ApiScheme.Scheme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class Character
    {
        /// <summary>
        /// Player who owns this Character.
        /// </summary>
        public Player Player { get; private set; }

        /// <summary>
        /// Room which belongs to.
        /// Can be null.
        /// </summary>
        public Room Room { get; set; }

        /// <summary>
        /// Name of Character.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// UserId of Player. (Check)
        /// </summary>
        public string UserId { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="info"></param>
        public Character(Player player, CharacterInfo info)
        {
            Player = player;
            Name = info.name;
            UserId = info.userId;
        }

        public override string ToString()
        {
            return Name;
            //return string.Format("[{0}({1})]", Name, Player.userId);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != GetType())
                return false;
            var c = (Character)obj;
            return c.Player == this.Player;
        }

        public static bool operator ==(Character a, Character b)
        {
            var oa = (object)a;
            var ob = (object)b;
            if (oa == null && ob == null)
                return true;
            if (oa == null || ob == null)
                return false;
            return a.Player == b.Player;
        }

        public static bool operator !=(Character a, Character b)
        {
            return !(a == b);
        }
    }
}
