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
            return string.Format("[Character Player:{0} Name:{1} UserId:{2}]", Player, Name, UserId);
        }
    }
}
