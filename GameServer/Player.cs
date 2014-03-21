using ApiScheme.Scheme;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// Represents a Player. Can be not authenticated.
    /// Player connects first and authenticate second for performance reasons.
    /// Not authenticated
    /// </summary>
    public class Player
    {
        /// <summary>
        /// SignalR ConnectionId.
        /// </summary>
        public string connectionId;

        /// <summary>
        /// MD5 hashed UserId. Displayed for players.
        /// </summary>
        public string userId;

        /// <summary>
        /// Prefered Language of Player. Default en-US.
        /// </summary>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// Player's Characters.
        /// </summary>
        public List<Character> characters = new List<Character>();

        public Player()
        {
            Culture = new CultureInfo("en-US");
        }

        /// <summary>
        /// Returns true if Player is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get
            {
                return userId != null;
            }
        }

        Character _character = null;
        /// <summary>
        /// Player's current Character.
        /// </summary>
        public Character Character
        {
            get
            {
                return _character;
            }
            set
            {
                if(IsAuthenticated)
                    _character = value;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;
            var b = (Player)obj;
            if (userId == null || b.userId == null)
                return false;
            return userId == b.userId;
        }

        public static bool operator ==(Player a, Player b)
        {
            if ((object)a == null && (object)b == null)
                return true;
            if ((object)a == null || (object)b == null)
                return false;
            return a.userId == b.userId;
        }
        public static bool operator !=(Player a, Player b)
        {
            if ((object)a == null && (object)b == null)
                return false;
            if ((object)a == null || (object)b == null)
                return true;
            return a.userId != b.userId;
        }

        public override string ToString()
        {
            return string.Format("[Player userId:{0} connectionId:{1}]", userId, connectionId);
        }

        /// <summary>
        /// Returns localized string for this Player.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetString(string key)
        {
            var str = MyResources._.ResourceManager.GetString(key, Culture);
            if (str == null)
                return key;
            return str;
        }
    }
}
