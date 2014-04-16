using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.LobbyCommand
{
    public abstract class Base
    {
        /// <summary>
        /// Player who has sent this Command.
        /// SYSTEM if null.
        /// </summary>
        public Player Sender { get; set; }
    }

    /// <summary>
    /// Notifies Lobby that Player joined a Room.
    /// (Maybe, Lobby removes this Player from _playersInLobby.)
    /// </summary>
    public class PlayerJoinedRoom : Base
    {
        public Player Player { get; set; }
    }
}
