using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.LobbyCommand
{
    public abstract class Base
    {
        public string ConnectionId { get; set; }
        /// <summary>
        /// Player who has sent this Command.
        /// SYSTEM if null.
        /// </summary>
        public Player Sender { get; set; }
    }

    // ----- Life Cycle -----
    public class OnConnected : Base
    {
        public dynamic Client { get; set; }
    }

    // cf. OnConnected
    /*public class OnReconnected : Base
    {

    }*/

    public class OnDisconnected : Base
    {
    }

    public class Authenticate : Base
    {
        public string Culture { get; set; }
        public string PassString { get; set; }
    }

    // ----- Character Selection -----
    public class GetCharacters : Base
    {

    }

    public class CreateCharacter : Base
    {
        public string ModelName { get; set; }
        public string Name { get; set; }
    }

    public class SelectCharacter : Base
    {
        public string Name { get; set; }
    }

    // ----- Lobby -----
    public class GetRooms : Base
    {

    }

    public class GetLobbyMessages : Base { }

    public class LobbySend : Base
    {
        public string Message { get; set; }
    }

    public class CreateRoom : Base
    {

    }

    public class JoinRoom : Base
    {
        public int RoomId { get; set; }
        public string Password { get; set; }
    }

    // ----- Internal Notification -----
    /// <summary>
    /// Notifies Lobby that Player joined a Room.
    /// (Maybe, Lobby removes this Player from _playersInLobby.)
    /// </summary>
    public class PlayerJoinedRoom : Base
    {
        public Player Player { get; set; }
    }
}
