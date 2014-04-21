using GameServer.ClientModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.RoomCommand
{
    public abstract class Base
    {
        public string ConnectionId { get; set; }
        /// <summary>
        /// Player who queued this RoomCommand.
        /// In another word, Caller.
        /// Null if queued by Anonymous.
        /// Player.System if queued by SYSTEM.
        /// </summary>
        public Player Sender { get; set; }
    }

    public class Send : Base
    {
        public int RoomSendMode { get; set; }
        public int ActorId { get; set; }
        public string Message { get; set; }
    }

    public class Report : Base
    {
        public int MessageId { get; set; }
        public string Note { get; set; }
    }

    public class AddCharacter : Base
    {
        public Character Character { get; set; }
        public string Password { get; set; }
    }

    public class SpectateCharacter : Base {
        public Character Character { get; set; }
    }

    public class RemovePlayer : Base
    {
        //public Player Target { get; set; }
    }

    public class Configure : Base
    {
        public Room.ClientConfiguration Configuration { get; set; }
    }

    public class Start : Base
    {
        public List<ClientRoleAmount> Roles { get; set; }
    }

    public class Vote : Base
    {
        public int ExecutionId { get; set; }
        public int AttackId { get; set; }
        public int FortuneTellId { get; set; }
        public int GuardId { get; set; }
    }

    // ----- RoomMaster -----
    public class Skip : Base {

    }

    public class Kick : Base {
        public string CharacterName { get; set; }
    }

    public class Kill : Base {
        public int ActorId { get; set; }
    }

    public class Revive : Base {
        public int ActorId { get; set; }
    }

    public class SetRole : Base {
        public int ActorId { get; set; }
        public Role Role { get; set; }
    }
}
