using Microsoft.AspNet.SignalR;
using MyResources;
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
        //public double EndingDuration { get; private set; }
        bool _needSync = false;

        int _nextMessageId = 0;
        List<RoomMessage> _messagesWillBeApplied = new List<RoomMessage>();
        List<RoomMessage> _messages = new List<RoomMessage>();

        ConcurrentQueue<RoomCommand.Base> _queue = new ConcurrentQueue<RoomCommand.Base>();

        public bool IsVisibleToJoin
        {
            get
            {
                return new RoomState[]{ RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState);
            }
        }
        /*public bool CanJoin
        {
            get
            {
                return
                    (new RoomState[] { RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState)
                    && _characters.Count < conf.max
                    && (RoomState == RoomState.Matchmaking && duration > 0))
                    || (RoomState == RoomState.Configuring && _characters.Count == 0);
            }
        }*/
        public bool CanJoin(Character character)
        {
            if (character == null)
                return false;

            if (_characters.Count == 0 && RoomState == RoomState.Configuring)
                // RoomMaster is coming.
                return true;

            if (!new RoomState[] { RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState))
                return false;

            if (RoomState == RoomState.Matchmaking && duration > 0)
                // Just starting
                return false;

            if (_characters.Count >= conf.max)
                return false;

            return true;
        }
        public RoomState RoomState { get; private set; }

        public bool IsEmpty
        {
            get
            {
                return _characters.Count == 0;
            }
        }

        public bool IsProcessingHistory { get; private set; }
        public bool RequiresPassword { get { return conf.password != null && conf.password.Length > 0; } }

        public bool ShouldBeDeleted
        {
            get { return RoomState!=RoomState.Ending && !IsProcessingHistory && _characters.Count == 0; }
        }

        List<RoomMessage.Mode> ModesFor(Actor actor)
        {
            if (actor == null)
                //throw new ArgumentNullException("actor must not be null.");
                return new List<RoomMessage.Mode>();
            var modes = new List<RoomMessage.Mode>();
            if (actor.IsDead)
            {
                // Dead
                modes.Add(RoomMessage.Mode.Ghost);
            }
            else
            {
                // Alive
                modes.Add(RoomMessage.Mode.All);
                modes.Add(RoomMessage.Mode.Private);
                switch (actor.role)
                {
                    case Role.Werewolf:
                        modes.Add(RoomMessage.Mode.Wolf);
                        break;
                }
            }
            return modes;
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
            return _characters.Any(c => c.Name == character.Name);
        }

        public bool IsRoomMaster(Actor actor)
        {
            if (actor == null)
                return false;
            if (actor.character == null)
                return false;
            var firstCharacter = _characters.FirstOrDefault();
            if(firstCharacter==null)
                return false;
            return actor.character.Player == firstCharacter.Player;
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

            switch (RoomState)
            {
                case RoomState.Matchmaking:
                    if (duration > 0)
                    {
                        duration -= MyHub.Elapsed;
                        if (duration <= 0)
                            Start();
                    }
                    break;

                case RoomState.Playing:
                    duration -= MyHub.Elapsed;
                    if (duration < 0)
                        GotoNextDay();
                    break;

                case RoomState.Ending:
                    duration -= MyHub.Elapsed;
                    if (duration < 0)
                    {
                        RoomState = RoomState.Ended;
                        //SystemMessageAll("Game has ended.");
                        SystemMessageAll(new InterText("GameHasEnded", _.ResourceManager));
                        _needSync = true;
                        //CallAll(client => client.broughtTo(ClientState.Rooms));
                    }
                    break;
            }

            ProcessMessages();

            if (_needSync)
                Sync();
        }

        void CallAll(Action<dynamic> action)
        {
            _characters.ForEach(c =>
            {
                var client = _updateHub.Clients.Client(c.Player.connectionId);
                action(client);
            });
        }

        void AddMessage(RoomMessage message)
        {
            message.id = _nextMessageId++;
            message.Created = DateTime.UtcNow;
            _messagesWillBeApplied.Add(message);
        }

        void SystemMessageTo(Actor to, InterText[] bodyRows)
        {
            AddMessage(new RoomMessage() { mode = RoomMessage.Mode.Private, to = to, bodyRows = bodyRows });
        }
        void SystemMessageTo(Actor to, InterText body)
        {
            SystemMessageTo(to, new [] { body });
        }

        void SystemMessageAll(InterText[] bodyRows)
        {
            AddMessage(new RoomMessage() { bodyRows = bodyRows });
        }
        void SystemMessageAll(InterText body)
        {
            AddMessage(new RoomMessage() { bodyRows = new[] { body } });
        }
        void SystemMessageAll(string message)
        {
            AddMessage(new RoomMessage() { bodyRows = new[] { new InterText(message, null) } });
        }

        void Sync()
        {
            _characters.ForEach(c =>
            {
                var client = _updateHub.Clients.Client(c.Player.connectionId);
                var yourActorId = new Nullable<int>();
                var yourActor = _actors.FirstOrDefault(a => a.IsOwnedBy(c.Player));
                if (yourActor != null)
                    yourActorId = yourActor.id;
                var actors = _actors.Select(a => new ActorInfo(this, c.Player, yourActor, a)).ToList();
                client.gotRoomState(RoomState);
                client.gotActors(actors);
                client.gotYourActorId(yourActorId);
                if (yourActor != null)
                    client.gotYourSelections(yourActor.VoteInfo);
                client.gotModes(ModesFor(yourActor).Select(m=>new RoomMessage.ModeInfo(c.Player, m)).ToList());
                client.gotTimer(duration);
            });
            _needSync = false;
        }

        void AddActorsForCharacters()
        {
            //_actors = Actor.Create(_characters.Count);
            var charactersWithoutActor = _characters.Where(c => !_actors.Any(a => a.character == c)).ToList();
            charactersWithoutActor.ForEach(c =>
            {
                var actor = Actor.CreateUnique(_actors);
                actor.character = c;
                _actors.Add(actor);
            });
        }
    }
}
