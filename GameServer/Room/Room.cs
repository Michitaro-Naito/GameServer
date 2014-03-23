using Microsoft.AspNet.SignalR;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// Represents a meeting of Players.
    /// Room is a pure simulator and independent on networking.
    /// Lobby and Players can interact with Room only by queues.
    /// Room notifies Players what happened in this Room using RPC.
    /// </summary>
    public partial class Room
    {
        public int roomId;
        public string guid;
        public Configuration conf = new Configuration();
        List<Character> _characters = new List<Character>();
        List<Actor> _actors = new List<Actor>();
        IHubContext _updateHub = null;

        public int day;
        public double duration;
        bool _needSync = false;

        ConcurrentQueue<RoomCommand.Base> _queue = new ConcurrentQueue<RoomCommand.Base>();

        public bool IsVisibleToJoin
        {
            get
            {
                return new RoomState[]{ RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState);
            }
        }
        public bool CanJoin
        {
            get
            {
                return
                    new RoomState[] { RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState)
                    && _actors.Any(a => a.character == null);
            }
        }
        public RoomState RoomState { get; private set; }

        public bool IsEmpty
        {
            get
            {
                return _characters.Count == 0;
            }
        }

        public Room()
        {
            guid = Guid.NewGuid().ToString();
            RoomState = RoomState.Configuring;
            day = 0;
            duration = 0;
        }

        public override string ToString()
        {
            return string.Format("[Room roomId:{0} guid:{1}]", roomId, guid);
        }

        public bool HasCharacter(Character character)
        {
            return _characters.Any(c => c == character);
        }

        /// <summary>
        /// Updates this Room.
        /// Thread-safe but can't be paralleled.
        /// Calling roomA.Update() and roomB.Update() the same time is OK.
        /// Calling roomA.Update() and roomA.Update() is not OK. (Just wasting CPU.)
        /// </summary>
        /// <param name="hub"></param>
        public void Update(IHubContext hub)
        {
            _updateHub = hub;

            ProcessQueues();

            if (RoomState == RoomState.Playing)
            {
                duration -= MyHub.Elapsed;
                if (duration < 0)
                    GotoNextDay();
            }

            if (_needSync)
                Sync();
        }

        void SystemMessageAll(string message)
        {
            _characters.ForEach(c =>
            {
                _updateHub.Clients.Client(c.Player.connectionId).addMessage("SYSTEM.Room", message);
            });
        }

        void Sync()
        {
            _characters.ForEach(c =>
            {
                var client = _updateHub.Clients.Client(c.Player.connectionId);
                var actors = _actors.Select(a => new ActorInfo(c.Player, a)).ToList();
                client.gotRoomState(RoomState);
                client.gotActors(actors);
            });
            _needSync = false;
        }
    }
}
