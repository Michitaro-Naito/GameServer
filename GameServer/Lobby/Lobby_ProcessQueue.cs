using ApiScheme.Client;
using ApiScheme.Scheme;
using MyResources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GameServer {
    partial class Lobby {
        void ProcessQueue() {
            // TODO: const
            /*var dic = new Dictionary<Type, Action<LobbyCommand.Base>>()
            {
                {typeof(LobbyCommand.OnConnected), OnConnected},
                {typeof(LobbyCommand.OnReconnected), OnReconnected},
                {typeof(LobbyCommand.OnDisconnected), OnDisconnected},
                {typeof(LobbyCommand.Authenticate), Authenticate},
                {typeof(LobbyCommand.GetCharacters), GetCharacters},
                {typeof(LobbyCommand.CreateCharacter), CreateCharacter},
                {typeof(LobbyCommand.SelectCharacter), SelectCharacter},
                {typeof(LobbyCommand.PlayerJoinedRoom), PlayerJoinedRoom},
            };*/

            LobbyCommand.Base commandBase;
            while (_queue.TryDequeue(out commandBase)) {
                try {
                    commandBase.Sender = GetPlayer(commandBase.ConnectionId);
                    var type = commandBase.GetType();
                    if (type != typeof(LobbyCommand.OnConnected) && commandBase.Sender == null)
                        // Sent by Unknown?
                        continue;
                    GetType().GetMethod(type.Name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, new object[] { commandBase });
                }
                catch (Exception e) {
                    // Unknown Error.
                    Logger.WriteLine(e.ToString());
                }
            }

            RoomCommand.Base roomCommandBase;
            while (_queueToRoom.TryDequeue(out roomCommandBase)) {
                try {
                    roomCommandBase.Sender = GetPlayer(roomCommandBase.ConnectionId);
                    if (roomCommandBase.Sender == null
                        || roomCommandBase.Sender.Character == null
                        || roomCommandBase.Sender.Character.Room == null)
                        // Sent by Unknown?
                        continue;
                    roomCommandBase.Sender.Character.Room.Queue(roomCommandBase);
                }
                catch (Exception e) {
                    // Unknown Error.
                    Logger.WriteLine(e.ToString());
                }
            }
        }

        void OnConnected(LobbyCommand.OnConnected command) {
            var p = new Player() { connectionId = command.ConnectionId, Client = command.Client };
            if (_players.Count >= 400)
                // Server is full.
                p.Client.gotDisconnectionRequest();
            else {
                // Accepts Player
                _players[p.connectionId] = p;
                p.GotBootTime(_bootTime);
                p.Client.addMessage("connected");
            }
        }

        void OnReconnected(LobbyCommand.OnReconnected command) {
            command.Sender.GotBootTime(_bootTime);
        }

        void OnDisconnected(LobbyCommand.OnDisconnected command) {
            Kick(command.Sender.userId);
        }

        void Authenticate(LobbyCommand.Authenticate command) {
            var player = command.Sender;

            var pass = AuthUtility.GamePass.FromCipher(command.PassString, ConfigurationManager.AppSettings["AesKey"], ConfigurationManager.AppSettings["AesIv"]);
            if (pass == null) {
                //SystemMessage("Invalid GamePass. Please login again.");
                player.GotSystemMessage("Invalid GamePass. Please login again.");
                return;
            }

            if (_blacklists.Any(b => b.infos.Any(info => info.userId == pass.data.userId))) {
                player.GotSystemMessage("You are banned.");
                player.Client.gotDisconnectionRequest();
                return;
            }

            // Kicks Players who have the same UserId
            Kick(pass.data.userId);

            // Accepts Player
            player.userId = pass.data.userId;
            try {
                player.Culture = new System.Globalization.CultureInfo(command.Culture);
            }
            catch {
                player.Culture = new System.Globalization.CultureInfo("en-US");
            }
            player.GotSystemMessage("Authenticated:" + pass.data.userId);

            // Passes Data
            player.Client.gotRoles(Enum.GetValues(typeof(Role)).Cast<Role>().Select(r => new RoleInfo(r, player.Culture)));
            player.Client.gotGenders(Enum.GetValues(typeof(Gender)).Cast<Gender>().Select(g => new GenderInfo(g, player.Culture)));
            player.Client.gotStrings(MyResources._UiString.ResourceManager.GetResourceSet(player.Culture, true, true));

            player.BroughtTo(ClientState.Characters);
        }

        void GetCharacters(LobbyCommand.GetCharacters command) {
            var p = command.Sender;
            p.GotSystemMessage("Getting Characters...");
            var o = Api.Get<GetCharactersOut>(new GetCharactersIn() { userId = p.userId });
            p.characters = o.characters.Select(ci => { return new Character(command.Sender, ci); }).ToList();
            p.GotSystemMessage("Got Characters.");
            o.characters.ForEach(c => {
                p.GotSystemMessage(c.name);
            });
            p.Client.gotCharacters(o.characters);
        }

        void CreateCharacter(LobbyCommand.CreateCharacter command) {
            var p = command.Sender;
            if (!p.IsAuthenticated) {
                p.GotSystemMessage("Not authenticated.");
                return;
            }
            var model = new ClientModel.ClientCreateCharacter() { ModelName = command.ModelName, name = command.Name };
            var result = model.Validate();
            var client = p.Client;
            if (!result.Success) {
                client.addMessage("Validation failed.");
                result.Errors.ForEach(e => client.addMessage(e.GetString(p.Culture)));
                client.gotValidationErrors(model.ModelName, result.Errors.Select(e => e.GetStringFor(p)));
                return;
            }
            p.GotSystemMessage("Creating a Character...");
            try {
                var o = Api.Get<CreateCharacterOut>(new CreateCharacterIn() { userId = p.userId, name = command.Name });
            }
            catch (ApiScheme.ApiMaxReachedException) {
                client.gotValidationErrors(model.ModelName, new List<string>() { _Error.ResourceManager.GetString("MaxReached", p.Culture) });
                return;
            }
            catch (ApiScheme.ApiNotUniqueException) {
                client.gotValidationErrors(model.ModelName, new List<string>() { _Error.ResourceManager.GetString("NotUnique", p.Culture) });
                return;
            }
            catch (Exception e) {
                // Something went wrong
                client.gotValidationErrors(model.ModelName, new List<string>() { "API returned error." + e });
                return;
            }
            p.GotSystemMessage("Created.");

            p.BroughtTo(ClientState.Characters);
        }

        void SelectCharacter(LobbyCommand.SelectCharacter command) {
            var p = command.Sender;
            var character = p.characters.FirstOrDefault(c => c.Name == command.Name);
            if (character == null) {
                p.GotSystemMessage("Character not Found");
                return;
            }
            p.Character = character;
            p.GotSystemMessage("Character found and selected.");
            p.BroughtTo(ClientState.Rooms);
        }

        void GetLobbyMessages(LobbyCommand.GetLobbyMessages command) {
            var p = command.Sender;
            if (p.Character == null)
                return;

            p.Client.gotLobbyMessages(_messages.Select(m => m.ToInfo(p)), true);
            p.Client.gotLobbyMessages(new[] { new LobbyMessage() { name = "SYSTEM", body = new InterText("WelcomeAChattingBPlayingCSelectingCharacterD", _.ResourceManager, new[] {
                new InterText(p.Character.Name, null),
                new InterText(_playersInLobby.Count.ToString(), null),
                new InterText(_playersInGame.Count.ToString(), null),
                new InterText(_players.Count(pl=>pl.Value.Character==null)/*PlayersWithoutCharacter.Count()*/.ToString(), null)
            }) } }.Select(m => m.ToInfo(p)));

            // Add Player to Lobby.
            if (!_playersInLobby.ContainsKey(p.connectionId)) {
                _playersInLobby[p.connectionId] = p;
                _playersInGame.Remove(p.connectionId);
            }
        }

        void GetRooms(LobbyCommand.GetRooms command) {
            var p = command.Sender;
            if (p.Character == null) {
                p.GotSystemMessage("Select Character first to get rooms.");
                return;
            }
            _rooms.ForEach(r => {
                if (r.IsVisibleToJoin)
                    p.GotSystemMessage(r.ToString());
                else
                    p.GotSystemMessage(r.ToString() + "(Hidden)");
            });
            var info = _rooms.Where(r => new[] { RoomState.Matchmaking, RoomState.Playing }.Contains(r.RoomState)).Select(r => r.ToInfo()).ToList();
            p.Client.gotRooms(info);
        }

        void CreateRoom(LobbyCommand.CreateRoom command) {
            var p = command.Sender;
            var index = 0;
            for (; ; index++) {
                if (_rooms.All(r => r.roomId != index))
                    break;
            }
            var room = new Room() { EnqueueLobby = Enqueue, roomId = index };
            _rooms.Add(room);
            p.GotSystemMessage(string.Format("Created. Room:{0}", room));
            BringPlayerToRoom(p, room.roomId, null);
        }

        void JoinRoom(LobbyCommand.JoinRoom command) {
            var p = command.Sender;
            if (p.Character == null) {
                p.GotSystemMessage("Select Character first to join room.");
                return;
            }
            BringPlayerToRoom(p, command.RoomId, command.Password);
        }

        void LobbySend(LobbyCommand.LobbySend command) {
            var p = command.Sender;
            if (p.Character == null)
                return;
            var newMessage = new LobbyMessage() { name = p.Character.Name, body = new InterText(command.Message, null) };
            _messages.Add(newMessage);
            while (_messages.Count > 50)
                _messages.RemoveAt(0);
            foreach (var t in _playersInLobby)
                t.Value.Client.gotLobbyMessages(new[] { newMessage }.Select(m => m.ToInfo(t.Value)));
        }

        void PlayerJoinedRoom(LobbyCommand.Base commandBase) {
            var command = (LobbyCommand.PlayerJoinedRoom)commandBase;
            _playersInLobby.Remove(command.Player.connectionId);
            _playersInGame[command.Player.connectionId] = command.Player;
        }
    }
}
