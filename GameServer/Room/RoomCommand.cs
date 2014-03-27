using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.RoomCommand
{
    public abstract class Base
    {
        /// <summary>
        /// Player who queued this RoomCommand.
        /// In another word, Caller.
        /// Null if queued by System.
        /// </summary>
        public Player Player { get; private set; }

        public Base(Player player)
        {
            Player = player;
        }
    }

    public class Send : Base
    {
        public int RoomSendMode { get; private set; }
        public int ActorId { get; private set; }
        public string Message { get; private set; }
        public Send(Player player, int roomSendMode, int actorId, string message)
            : base(player)
        {
            RoomSendMode = roomSendMode;
            ActorId = actorId;
            Message = message;
        }
    }

    public class AddCharacter : Base
    {
        public Character Character { get; private set; }

        public AddCharacter(Player player, Character character)
            : base(player)
        {
            Character = character;
        }
    }

    public class RemovePlayer : Base
    {
        public Player Target { get; private set; }

        public RemovePlayer(Player player, Player target)
            : base(player)
        {
            Target = target;
        }
    }

    public class Configure : Base
    {
        public Room.ClientConfiguration Configuration { get; private set; }

        public Configure(Player player, Room.ClientConfiguration configuration)
            : base(player)
        {
            Configuration = configuration;
        }
    }

    public class Start : Base
    {
        public Start(Player player)
            : base(player)
        {
        }
    }

    public class Vote : Base
    {
        public int ExecutionId { get; private set; }
        public int AttackId { get; private set; }
        public int FortuneTellId { get; private set; }
        public int GuardId { get; private set; }

        public Vote(Player player, int executionId, int attackId, int fortuneTellId, int guardId)
            : base(player)
        {
            ExecutionId = executionId;
            AttackId = attackId;
            FortuneTellId = fortuneTellId;
            GuardId = guardId;
        }
    }
}
