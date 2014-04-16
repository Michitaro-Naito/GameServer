using ApiScheme.Client;
using ApiScheme.Scheme;
using Microsoft.AspNet.SignalR;
using MyResources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameServer
{
    public class MyHub : Hub
    {
        // ----- Static Variable -----
        static DateTime _bootTime = DateTime.UtcNow;
        static DateTime _lastUpdate = DateTime.UtcNow;
        public static double Elapsed { get; private set; }

        static Dictionary<string, Player> _players = new Dictionary<string, Player>();
        static Dictionary<string, Player> _playersInLobby = new Dictionary<string, Player>();
        static Dictionary<string, Player> _playersInGame = new Dictionary<string, Player>();
        static List<Room> _rooms = new List<Room>();
        static List<LobbyMessage> _messages = new List<LobbyMessage>();

        static List<GetBlacklistOut> _blacklists = new List<GetBlacklistOut>();
        static int _nexBlacklistPage = 0;
        static double _durationUntilNextPage = 0;

        static ConcurrentQueue<LobbyCommand.Base> _queue = new ConcurrentQueue<LobbyCommand.Base>();



        // ----- Property -----

        /// <summary>
        /// Returns current Player.
        /// </summary>
        public Player Player
        {
            get
            {
                // KeyNotFoundException
                return _players[Context.ConnectionId];
            }
        }

        /// <summary>
        /// Returns current Character.
        /// </summary>
        public Character Character { get { return Player.Character; } }

        // PFM
        public Room Room
        {
            get
            {
                if (Character == null)
                    return null;
                return _rooms.FirstOrDefault(r => r.HasCharacter(Character));
            }
        }

        // PFM
        public IEnumerable<Player> PlayersWithoutCharacter
        {
            get
            {
                return _players.Where(p => p.Value.Character == null).Select(en=>en.Value);
            }
        }



        // ----- Override -----
        
        public override Task OnConnected()
        {
            if (_players.Count >= 400)
            {
                // Server is full.
                Clients.Caller.gotDisconnectionRequest();
            }
            else
            {
                // Accepts Player
                var p = new Player() { connectionId = Context.ConnectionId, Client = Clients.Caller };
                _players[Context.ConnectionId] = p;
                Clients.Caller.gotBootTime(_bootTime);
            }

            return base.OnConnected();
        }

        public override Task OnReconnected()
        {
            Clients.Caller.gotBootTime(_bootTime);
            return base.OnReconnected();
        }

        public override Task OnDisconnected()
        {
            Kick(Player.userId);
            return base.OnDisconnected();
        }



        // ----- Static Method -----
        public static void Update()
        {
            var now = DateTime.UtcNow;
            Elapsed = (now - _lastUpdate).TotalSeconds;
            _lastUpdate = now;
            //var hub = GlobalHost.ConnectionManager.GetHubContext<MyHub>();

            // Updates Hub
            ProcessQueues();

            _durationUntilNextPage -= Elapsed;
            if (_durationUntilNextPage < 0)
            {
                Console.WriteLine("Getting page " + _nexBlacklistPage);
                var blacklist = Api.Get<GetBlacklistOut>(new GetBlacklistIn() { page = _nexBlacklistPage });
                if (_blacklists.Count > _nexBlacklistPage)
                    _blacklists[_nexBlacklistPage] = blacklist;
                else
                    _blacklists.Add(blacklist);
                _nexBlacklistPage++;

                var str = "";
                _blacklists.ForEach(b => b.infos.ForEach(info => str += info.userId + ","));
                Console.WriteLine("CurrentBlacklist: " + str);

                blacklist.infos.ForEach(info =>
                {
                    Kick(info.userId);
                });

                if (blacklist.infos.Count == 0)
                    _nexBlacklistPage = 0;
                _durationUntilNextPage = 10;
            }

            // Updates Rooms
            _rooms.ForEach(r => r.Update());

            // Cleans Rooms
            _rooms.RemoveAll(r => r.ShouldBeDeleted);
        }

        static void ProcessQueues()
        {
            LobbyCommand.Base commandBase;
            while (_queue.TryDequeue(out commandBase))
            {
                var type = commandBase.GetType();

                if (type == typeof(LobbyCommand.PlayerJoinedRoom))
                {
                    var command = (LobbyCommand.PlayerJoinedRoom)commandBase;
                    _playersInLobby.Remove(command.Player.connectionId);
                    _playersInGame[command.Player.connectionId] = command.Player;
                }
            }
        }



        // ----- Method ( Client to Server ) -----

        /// <summary>
        /// Authenticates Player using GamePass.
        /// </summary>
        /// <param name="gamePass"></param>
        public void Authenticate(string culture, string passString)
        {
            var player = Player;

            var pass = AuthUtility.GamePass.FromCipher(passString, ConfigurationManager.AppSettings["AesKey"], ConfigurationManager.AppSettings["AesIv"]);
            if (pass == null)
            {
                SystemMessage("Invalid GamePass. Please login again.");
                return;
            }

            if (_blacklists.Any(b => b.infos.Any(info => info.userId == pass.data.userId)))
            {
                SystemMessage("You are banned.");
                Clients.Caller.gotDisconnectionRequest();
                return;
            }

            // Kicks Players who have the same UserId
            Kick(pass.data.userId);

            // Accepts Player
            player.userId = pass.data.userId;
            try
            {
                player.Culture = new System.Globalization.CultureInfo(culture);
            }
            catch
            {
                player.Culture = new System.Globalization.CultureInfo("en-US");
            }
            SystemMessage("Authenticated:" + pass.data.userId);

            // Passes Data
            Clients.Caller.gotRoles(Enum.GetValues(typeof(Role)).Cast<Role>().Select(r=>new RoleInfo(r, player.Culture)));
            Clients.Caller.gotGenders(Enum.GetValues(typeof(Gender)).Cast<Gender>().Select(g => new GenderInfo(g, player.Culture)));
            Clients.Caller.gotStrings(MyResources._UiString.ResourceManager.GetResourceSet(player.Culture, true, true));

            BroughtTo(ClientState.Characters);
        }

        public void Send(string message)
        {
            if (message == null || message.Length == 0)
                return;

            if (!Player.IsAuthenticated)
            {
                SystemMessage("You are not authenticated.");
                Clients.Caller.gotDisconnectionRequest();
                return;
            }
            if (message.StartsWith("/"))
            {
                var match = Regex.Match(message, @"^/(?<command>\w+)(\s(?<params>\w+))*");
                if (match.Success)
                {
                    try
                    {
                        var command = match.Groups["command"].ToString();
                        var param = new List<string>();
                        foreach (var c in match.Groups["params"].Captures)
                        {
                            param.Add(c.ToString());
                        }
                        switch (command)
                        {
                            case "GetCharacters":
                                GetCharacters();
                                break;
                            case "SelectCharacter":
                                SelectCharacter(param[0]);
                                break;
                            case "GetRooms":
                                GetRooms();
                                break;
                            case "CreateRoom":
                                CreateRoom();
                                break;
                            case "QuitRoom":
                                QuitRoom();
                                break;
                            default:
                                SystemMessage(string.Format("Unknown command: {0}", command));
                                break;
                        }
                    }
                    catch(Exception e)
                    {
                        SystemMessage(e.ToString());
                    }
                    return;
                }
            }
            Clients.Caller.addMessage(Player.userId, message);
        }

        public void LobbySend(string message)
        {
            if (Character == null)
                return;
            var newMessage = new LobbyMessage() { name = Character.Name, body = new InterText(message, null) };
            _messages.Add(newMessage);
            while (_messages.Count > 50)
                _messages.RemoveAt(0);
            foreach(var p in _playersInLobby)
                p.Value.Client.gotLobbyMessages(new[] { newMessage }.Select(m => m.ToInfo(p.Value)));
        }

        public void RoomSend(int roomSendMode, int actorId, string message)
        {
            var room = Room;
            if (room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            room.Queue(new RoomCommand.Send(Player, roomSendMode, actorId, message));
        }

        public void RoomReportMessage(int messageId, string note)
        {
            if (Room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            SystemMessage("Queueing to report..." + messageId + note);
            Room.Queue(new RoomCommand.Report(Player, messageId, note));
        }

        public void RoomConfigure(Room.ClientConfiguration conf)
        {
            var room = Room;
            if (room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            room.Queue(new RoomCommand.Configure(Player, conf));
        }

        public void RoomStart()
        {
            var room = Room;
            if (room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            room.Queue(new RoomCommand.Start(Player));
        }

        public void RoomVote(int executionId, int attackId, int fortuneTellId, int guardId)
        {
            var room = Room;
            if (room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            SystemMessage("Voting..." + executionId);
            room.Queue(new RoomCommand.Vote(Player, executionId, attackId, fortuneTellId, guardId));
        }

        void GetCharacters()
        {
            SystemMessage("Getting Characters...");
            var o = Api.Get<GetCharactersOut>(new GetCharactersIn() { userId = Player.userId });
            Player.characters = o.characters.Select(ci=>{return new Character(Player, ci);}).ToList();
            SystemMessage("Got Characters.");
            o.characters.ForEach(c =>
            {
                SystemMessage(c.name);
            });
            Clients.Caller.gotCharacters(o.characters);
        }

        public void CreateCharacter(string modelName, string name)
        {
            if (!Player.IsAuthenticated)
            {
                SystemMessage("Not authenticated.");
                return;
            }
            var model = new ClientModel.ClientCreateCharacter() { ModelName = modelName, name = name };
            var result = model.Validate();
            var client = Clients.Caller;
            if (!result.Success)
            {
                client.addMessage("Validation failed.");
                result.Errors.ForEach(e => client.addMessage(e.GetString(Player.Culture)));
                client.gotValidationErrors(model.ModelName, result.Errors.Select(e => e.GetStringFor(Player)));
                return;
            }
            SystemMessage("Creating a Character...");
            try
            {
                var o = Api.Get<CreateCharacterOut>(new CreateCharacterIn() { userId = Player.userId, name = name });
            }
            catch (ApiScheme.ApiMaxReachedException)
            {
                client.gotValidationErrors(model.ModelName, new List<string>() { _Error.ResourceManager.GetString("MaxReached", Player.Culture) });
                return;
            }
            catch (ApiScheme.ApiNotUniqueException)
            {
                client.gotValidationErrors(model.ModelName, new List<string>() { _Error.ResourceManager.GetString("NotUnique", Player.Culture) });
                return;
            }
            catch(Exception e)
            {
                // Something went wrong
                client.gotValidationErrors(model.ModelName, new List<string>(){ "API returned error." + e });
                return;
            }
            SystemMessage("Created.");

            BroughtTo(ClientState.Characters);
        }

        void SelectCharacter(string name)
        {
            var character = Player.characters.FirstOrDefault(c => c.Name == name);
            if (character == null)
            {
                SystemMessage("Character not Found");
                return;
            }
            Player.Character = character;
            SystemMessage("Character found and selected.");
            BroughtTo(ClientState.Rooms);
            /*Clients.Caller.gotLobbyMessages(_messages.Select(m=>m.ToInfo(Player)), true);
            Clients.Caller.gotLobbyMessages(new[] { new LobbyMessage() { name = "SYSTEM", body = new InterText("WelcomeAChattingBPlayingCSelectingCharacterD", _.ResourceManager, new[] {
                new InterText(Player.Character.Name, null),
                new InterText(_playersInLobby.Count.ToString(), null),
                new InterText(_playersInGame.Count.ToString(), null),
                new InterText(PlayersWithoutCharacter.Count().ToString(), null)
            }) } }.Select(m => m.ToInfo(Player)));*/
        }

        public void GetLobbyMessages()
        {
            if (Character == null)
                return;

            Clients.Caller.gotLobbyMessages(_messages.Select(m=>m.ToInfo(Player)), true);
            Clients.Caller.gotLobbyMessages(new[] { new LobbyMessage() { name = "SYSTEM", body = new InterText("WelcomeAChattingBPlayingCSelectingCharacterD", _.ResourceManager, new[] {
                new InterText(Player.Character.Name, null),
                new InterText(_playersInLobby.Count.ToString(), null),
                new InterText(_playersInGame.Count.ToString(), null),
                new InterText(PlayersWithoutCharacter.Count().ToString(), null)
            }) } }.Select(m => m.ToInfo(Player)));

            // Add Player to Lobby.
            if (!_playersInLobby.ContainsKey(Player.connectionId))
            {
                _playersInLobby[Player.connectionId] = Player;
                _playersInGame.Remove(Player.connectionId);
            }
        }

        void GetRooms()
        {
            if (Player.Character == null)
            {
                SystemMessage("Select Character first to get rooms.");
                return;
            }
            _rooms.ForEach(r =>
            {
                if (r.IsVisibleToJoin)
                    SystemMessage(r.ToString());
                else
                    SystemMessage(r.ToString() + "(Hidden)");
            });
            var info = _rooms.Where(r=>new []{RoomState.Matchmaking, RoomState.Playing}.Contains(r.RoomState)).Select(r => r.ToInfo()).ToList();
            Clients.Caller.gotRooms(info);
        }

        /// <summary>
        /// Creates a Room and join.
        /// </summary>
        void CreateRoom()
        {
            var index = 0;
            for (; ; index++)
            {
                if (_rooms.All(r => r.roomId != index))
                    break;
            }
            var room = new Room() { EnqueueLobby = Enqueue, roomId = index };
            _rooms.Add(room);
            SystemMessage(string.Format("Created. Room:{0}", room));
            BringPlayerToRoom(room.roomId, null);
        }

        public void JoinRoom(int roomId, string password)
        {
            if (Player.Character == null)
            {
                SystemMessage("Select Character first to join room.");
                return;
            }
            BringPlayerToRoom(roomId, password);
        }

        void QuitRoom()
        {
            var room = Room;
            SystemMessage(string.Format("Quitting from {0}", room));
            room.Queue(new RoomCommand.RemovePlayer(Player, Player));
        }



        // ----- Method ( Server to Client ) -----

        void SystemMessage(string message)
        {
            Clients.Caller.addMessage("SYSTEM", Player.GetString(message));
        }

        void SystemMessage(Player player, string message)
        {
            Clients.Client(player.connectionId).addMessage("SYSTEM", Player.GetString(message));
        }

        void BroughtTo(ClientState state)
        {
            Clients.Caller.broughtTo(state);
        }



        // ----- Method (Utility) -----

        internal void Enqueue(LobbyCommand.Base command)
        {
            Console.WriteLine("Enqueued");
            _queue.Enqueue(command);
        }

        void BringPlayerToRoom(int roomId, string password)
        {
            var room = _rooms.FirstOrDefault(r => r.roomId == roomId);
            if (room == null)
            {
                SystemMessage("Room not found:" + roomId);
                return;
            }

            if (Player.Character == null)
            {
                SystemMessage("Failded to join Room. Character not selected.");
                return;
            }

            _rooms.ForEach(r => r.Queue(new RoomCommand.RemovePlayer(Player, Player)));
            room.Queue(new RoomCommand.AddCharacter(Player, Player.Character, password));
        }

        /// <summary>
        /// Kicks Player from this server.
        /// </summary>
        /// <param name="userId"></param>
        internal static void Kick(string userId)
        {
            // Kicks from Room
            _rooms.ForEach(r => r.Kick(userId));

            // Kicks from Lobby
            var keysToRemove = new List<string>();
            _players.Where(en => en.Value.userId == userId).ToList().ForEach(en =>
            {
                keysToRemove.Add(en.Key);
                en.Value.Client.gotDisconnectionRequest();
            });
            keysToRemove.ForEach(key =>
            {
                _players.Remove(key);
                _playersInLobby.Remove(key);
                _playersInGame.Remove(key);
            });
        }
    }
}
