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
        public Action<LobbyCommand.Base> EnqueueLobby = null;
        public int roomId;
        public string guid;
        public Configuration conf = new Configuration();

        /// <summary>
        /// Characters who are playing in this Room.
        /// (Absent Characters excluded.)
        /// </summary>
        List<Character> _characters = new List<Character>();

        /// <summary>
        /// Characters who are spectating in this Room.
        /// Spectators just get Actors and Messages. Does nothing.
        /// </summary>
        List<Character> _spectators = new List<Character>();

        IEnumerable<Character> CharactersAndSpectators {
            get { return _characters.Concat(_spectators); }
        }

        /// <summary>
        /// Actors in this Room.
        /// </summary>
        List<Actor> _actors = new List<Actor>();
        //IHubContext _updateHub = null;

        public int day;
        public double duration;
        public Faction FactionWon { get; set; }
        bool _needSync = false;

        int _nextMessageId = 0;
        List<RoomMessage> _messagesWillBeApplied = new List<RoomMessage>();
        List<RoomMessage> _messages = new List<RoomMessage>();

        ConcurrentQueue<RoomCommand.Base> _queue = new ConcurrentQueue<RoomCommand.Base>();

        public bool IsVisibleToJoin
        {
            get
            {
                return new []{ RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState);
            }
        }

        public bool CanJoin(Character character)
        {
            if (character == null)
                return false;

            if (RoomState != RoomState.Ended && _actors.Any(a => a.IsOwnedBy(character.Player)))
                // Already there.
                return true;

            switch (RoomState) {
                case RoomState.Configuring:
                    return _characters.Count == 0;

                case RoomState.Matchmaking:
                    if (duration > 0)
                        return false;
                    return _characters.Count < conf.max;

                case RoomState.Playing:
                    return AliveNPCs.Count() > 0;

                default:
                    return false;
            }

            /*if (_characters.Count == 0 && RoomState == RoomState.Configuring)
                // RoomMaster is coming.
                return true;

            if (!new RoomState[] { RoomState.Matchmaking, RoomState.Playing, RoomState.Ending }.Contains(RoomState))
                return false;

            if (_actors.Any(a => a.IsOwnedBy(character.Player)))
                // Already there.
                return true;

            if (RoomState == RoomState.Matchmaking && duration > 0)
                // Just starting
                return false;

            if (RoomState == RoomState.Matchmaking && _characters.Count >= conf.max)
                // Full
                return false;

            if (RoomState==RoomState.Playing && AliveNPCs.Count() == 0)
                // No empty slot
                return false;

            return true;*/
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
            get { return RoomState!=RoomState.Ending && !IsProcessingHistory && CharactersAndSpectators.Count() == 0; }
        }

        List<RoomMessage.Mode> ModesFor(Actor actor)
        {
            if (actor == null)
                return new List<RoomMessage.Mode>();
            var modes = new List<RoomMessage.Mode>();
            if (actor.IsDead)
            {
                // Dead
                modes.Add(RoomMessage.Mode.Ghost);
                // DeadRoomMaster can send All
                if (IsRoomMaster(actor))
                    modes.Add(RoomMessage.Mode.All);
            }
            else
            {
                // Alive
                modes.Add(RoomMessage.Mode.All);
                modes.Add(RoomMessage.Mode.Private);
                /*switch (actor.role)
                {
                    case Role.Werewolf:
                        modes.Add(RoomMessage.Mode.Wolf);
                        break;
                }*/
                if(actor.CanShareWerewolfCommunity)
                    modes.Add(RoomMessage.Mode.Wolf);
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

        public bool IsRoomMaster(Character character) {
            if (character == null)
                return false;
            return character == _characters.FirstOrDefault();
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
        public void Update(double elapsed)
        {
            //_updateHub = hub;

            ProcessQueues();

            switch (RoomState)
            {
                case RoomState.Matchmaking:
                    if (duration > 0)
                    {
                        duration -= elapsed;
                        if (duration <= 0)
                            Start();
                    }
                    break;

                case RoomState.Playing:
                    duration -= elapsed;
                    if (duration < 0)
                        GotoNextDay();
                    break;

                case RoomState.Ending:
                    duration -= elapsed;
                    if (duration < 0)
                    {
                        RoomState = RoomState.Ended;
                        SystemMessageAll(new InterText("GameHasEnded", _.ResourceManager));

                        // Save Win/Lose info.
                        SavePerks();

                        // Saves logs to Azure Blob Storage...
                        SaveLogs();

                        _needSync = true;
                    }
                    break;
            }

            ProcessMessages();

            if (_needSync)
                Sync();
        }

        void CallAll(Action<dynamic> action)
        {
            _characters.ForEach(c => action(c.Player.Client));
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

        void SystemMessageWolf(InterText[] bodyRows) {
            AddMessage(new RoomMessage() { mode= RoomMessage.Mode.Wolf, bodyRows = bodyRows });
        }

        void Sync()
        {
            //_characters.ForEach(c =>
            CharactersAndSpectators.ToList().ForEach(c =>
            {
                var client = c.Player.Client;// _updateHub.Clients.Client(c.Player.connectionId);
                var yourActorId = new Nullable<int>();
                var yourActor = _actors.FirstOrDefault(a => a.IsOwnedBy(c.Player));
                if (yourActor != null && _characters.Contains(c))   // yourActor = null if spectating
                    yourActorId = yourActor.id;
                var actors = _actors.Select(a => a.ToInfo(this, c.Player, yourActor) /*new ActorInfo(this, c.Player, yourActor, a)*/).ToList();
                client.gotRoomConfigurations(conf);
                client.gotRoomState(RoomState);
                client.gotActors(actors);
                client.gotYourActorId(yourActorId);
                if (yourActor != null)
                    client.gotYourSelections(yourActor.VoteInfo);
                client.gotModes(ModesFor(yourActor).Select(m=>new RoomMessage.ModeInfo(c.Player, m)).ToList());
                client.gotTimer(duration);
                client.gotFactionWon(FactionWon);
            });
            _needSync = false;
        }

        void AddActorsForCharacters()
        {
            var charactersWithoutActor = _characters.Where(c => !_actors.Any(a => a.character == c)).ToList();
            charactersWithoutActor.ForEach(c =>
            {
                var actor = Actor.CreateUnique(_actors);
                actor.character = c;
                _actors.Add(actor);
                SystemMessageAll(new InterText("AHasComeAsB", MyResources._.ResourceManager, new[] { new InterText(c.Name, null), actor.TitleAndName }));
            });
        }

        IEnumerable<RoomMessageInfo> GetOlderMessagesFor(Character character, int? currentOldestId) {
            if (character == null || character.Player == null)
                return new List<RoomMessageInfo>();

            Actor actor = null;
            if (_characters.Contains(character))
                // Not Spectator
                actor = _actors.FirstOrDefault(a => a.IsOwnedBy(character.Player));

            IEnumerable<RoomMessage> q = _messages.OrderByDescending(m => m.id);
            if (currentOldestId != null)
                q = q.SkipWhile(m => m.id >= currentOldestId.Value);
            var messages = q.Where(m=>m.IsVisibleFor(this, actor)).Take(50).OrderBy(m => m.id)
                .Select(m => new RoomMessageInfo(m, character.Player.Culture));
            return messages;
        }

        void SendFirstMessagesTo(/*Actor actor*/Character character)
        {
            if (character.Player == null)
                return;
            character.Player.Client.gotRoomMessages(GetOlderMessagesFor(character, null), true);
            /*if (actor != null && actor.character != null)
            {
                var client = actor.character.Player.Client;
                client.gotRoomMessages(
                    GetOlderMessagesFor(actor.character, null),
                    true);
            }*/
        }
        /*void SendFirstMessagesTo(Character spectator) {
            spectator.Player.Client.gotRoomMessages(
                GetOlderMessagesFor(spectator, null),
                true);
        }*/
    }
}
