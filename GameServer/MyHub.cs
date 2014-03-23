using ApiScheme.Client;
using ApiScheme.Scheme;
using Microsoft.AspNet.SignalR;
using System;
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
        static List<Player> _players = new List<Player>();
        static List<Room> _rooms = new List<Room>();
        static DateTime _lastUpdate = DateTime.UtcNow;
        public static double Elapsed { get; private set; }



        // ----- Property -----

        /// <summary>
        /// Cached current Player.
        /// </summary>
        Player _player = null;

        /// <summary>
        /// Returns current Player.
        /// </summary>
        public Player Player
        {
            get
            {
                if (_player != null)
                    return _player;
                _player = _players.FirstOrDefault(p => p.connectionId == Context.ConnectionId);
                if (_player == null)
                    _player = new Player();
                return _player;
            }
        }

        /// <summary>
        /// Returns current Character.
        /// </summary>
        public Character Character
        {
            get
            {
                return Player.Character;
            }
        }

        public Room Room
        {
            get
            {
                if (Character == null)
                    return null;
                return _rooms.FirstOrDefault(r => r.HasCharacter(Character));
            }
        }



        // ----- Override -----
        
        public override Task OnConnected()
        {
            var p = new Player() { connectionId = Context.ConnectionId };
            _players.Add(p);
            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            _players.RemoveAll(p => p.connectionId == Context.ConnectionId);
            return base.OnDisconnected();
        }



        // ----- Static Method -----
        public static void Update()
        {
            var now = DateTime.UtcNow;
            Elapsed = (now - _lastUpdate).TotalSeconds;
            _lastUpdate = now;
            var hub = GlobalHost.ConnectionManager.GetHubContext<MyHub>();
            _rooms.ForEach(r => r.Update(hub));
        }



        // ----- Method ( Client to Server ) -----

        /// <summary>
        /// Authenticates Player using GamePass.
        /// </summary>
        /// <param name="gamePass"></param>
        public void Authenticate(string culture, string passString)
        {
            var player = _players.FirstOrDefault(p => p.connectionId == Context.ConnectionId);
            if (player == null)
            {
                SystemMessage("You are not connected.");
                return;
            }

            var pass = AuthUtility.GamePass.FromCipher(passString, ConfigurationManager.AppSettings["AesKey"], ConfigurationManager.AppSettings["AesIv"]);
            if (pass == null)
            {
                SystemMessage("Invalid GamePass. Please login again.");
                return;
            }

            player.userId = pass.data.userId;
            player.Culture = new System.Globalization.CultureInfo(culture);
            SystemMessage("Authenticated:" + pass.data.userId);
            BroughtTo(ClientState.Characters);
        }

        public void Send(string message)
        {
            if (message == null || message.Length == 0)
                return;

            if (!Player.IsAuthenticated)
            {
                SystemMessage("You are not authenticated.");
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
                            case "CreateCharacter":
                                CreateCharacter(param[0]);
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
                            case "JoinRoom":
                                JoinRoom(int.Parse(param[0]));
                                break;
                            case "QuitRoom":
                                QuitRoom();
                                break;
                            case "Room":
                                CallRoom(param);
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
            Clients.All.addMessage(Player.userId, message);
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

        void CreateCharacter(string name)
        {
            SystemMessage("Creating a Character...");
            var o = Api.Get<CreateCharacterOut>(new CreateCharacterIn() { userId = Player.userId, name = name });
            SystemMessage("Created.");
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
            var info = _rooms.Select(r => new RoomInfo(r)).ToList();
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
            var room = new Room() { roomId = index };
            _rooms.Add(room);
            SystemMessage(string.Format("Created. Room:{0}", room));
            BringPlayerToRoom(room.roomId);
        }

        /// <summary>
        /// Join a Room.
        /// </summary>
        /// <param name="roomId"></param>
        void JoinRoom(int roomId)
        {
            BringPlayerToRoom(roomId);
        }

        void QuitRoom()
        {
            var room = Room;
            SystemMessage(string.Format("Quitting from {0}", room));
            room.RemoveAll(Player);
            if (room.IsEmpty)
                _rooms.Remove(room);
            BroughtTo(ClientState.Rooms);
        }

        void CallRoom(List<string> parameters)
        {
            var room = Room;
            if (room == null)
            {
                SystemMessage("Join Room first.");
                return;
            }
            room.Command(this, Character, parameters);
        }



        // ----- Method ( Server to Client ) -----

        public void SystemMessage(string message)
        {
            Clients.Caller.addMessage("SYSTEM", Player.GetString(message));
        }

        public void SystemMessage(Player player, string message)
        {
            Clients.Client(player.connectionId).addMessage("SYSTEM", Player.GetString(message));
        }

        public void BroughtTo(ClientState state)
        {
            Clients.Caller.broughtTo(state);
        }



        // ----- Method (Utility) -----

        void BringPlayerToRoom(int roomId)
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

            if (!room.IsEmpty && !room.CanJoin)
            {
                SystemMessage("RoomMaster is configuring Room. Couldn't join at this moment.");
                return;
            }

            _rooms.ForEach(r => r.RemoveAll(Player));
            room.Add(this, Player.Character);
            SystemMessage("Joined.");
            BroughtTo(ClientState.Playing);
        }
    }
}
