﻿using Microsoft.AspNet.SignalR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public enum RoomState
    {
        Configuring         = 0000,
        Matchmaking         = 0001,
        Playing             = 0002,
        Ending              = 0003,
        Ended               = 0004
    }

    public class RoomInfo
    {
        public int roomId;
        public string guid;
        public string name;
        public int max;
        public int interval;

        public RoomInfo(Room room)
        {
            roomId = room.roomId;
            guid = room.guid;
            name = room.name;
            max = room.max;
            interval = room.interval;
        }
    }

    public partial class Room
    {
        public int roomId;
        public string guid;
        public string name;
        public int max;
        public int interval;
        List<Character> _characters = new List<Character>();
        List<Actor> _actors = new List<Actor>();
        IHubContext _updateHub = null;

        public int day;
        public double duration;

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

        public void Add(MyHub hub, Character character)
        {
            _characters.Add(character);
            var actor = _actors.Where(a => a.character == null).RandomElement();
            if (actor != null)
            {
                actor.character = character;
                SystemMessageAll(string.Format("{0} joined as {1}", _characters, actor));
            }
            //CallPlayer(character.Player, c => c.gotRoomState(RoomState));
            hub.Clients.Caller.gotRoomState(RoomState);
        }

        public void RemoveAll(Player player)
        {
            _characters.RemoveAll(c => c.Player == player);
            _actors.RemoveAll(a =>
            {
                return a.character != null && a.character.Player == player;
            });
        }

        public void Command(MyHub hub, Character character, List<string> parameters)
        {
            hub.SystemMessage("Room.Command()");
            if (parameters.Count == 0)
            {
                hub.SystemMessage("No params...");
                return;
            }
            switch (parameters[0])
            {
                case "GetCharacters":
                    hub.SystemMessage(string.Format("Characters at {0}:", this));
                    _characters.ForEach(c => hub.SystemMessage(c.ToString()));
                    break;
                /*case "GetActors":
                    hub.SystemMessage(string.Format("Actors at {0}:", this));
                    var info = _actors.Select(a => new ActorInfo(a)).ToList();
                    hub.Clients.Caller.gotActors(info);
                    break;*/
                case "Chat":
                    _characters.ForEach(c => hub.SystemMessage(c.Player, parameters[1]));
                    break;
                case "Configure":
                    Configure(hub, parameters[1], parameters[2], parameters[3]);
                    break;
                case "Start":
                    if(RoomState != RoomState.Matchmaking)
                    {
                        hub.SystemMessage("Could not start. RoomState must be matchmaking: "+RoomState);
                        break;
                    }
                    var count = Math.Max(7, _characters.Count);

                    // Remove NPC until count
                    while (_actors.Count > 7 && _actors.Any(a => a.character == null))
                    {
                        var npcToRemove = _actors.Where(a => a.character == null).RandomElement();
                        _actors.Remove(npcToRemove);
                    }

                    // Casts Roles
                    var dic = RoleHelper.CastRolesAuto(count);
                    foreach (var p in dic)
                    {
                        for (var n = 0; n < p.Value; n++)
                            _actors.Where(a => a.role == Role.None).RandomElement().role = p.Key;
                    }

                    // Changes State
                    RoomState = RoomState.Playing;
                    duration = interval;

                    //CallPlayers(c => c.gotRoomState(RoomState.Playing));
                    Sync();
                    hub.SystemMessage(string.Format("Game started at {0}", this));
                    break;
                default:
                    hub.SystemMessage("Unknown RoomCommand: " + parameters[0]);
                    break;
            }
        }

        public void Configure(MyHub hub, string name, string max, string interval)
        {
            if (RoomState != RoomState.Configuring)
                return;

            this.name = name;
            this.max = int.Parse(max);
            this.interval = int.Parse(interval);

            _actors = Actor.Create(this.max);
            // Assigns Characters to Actors
            _characters.ForEach(c =>
            {
                var npcActor = _actors.FirstOrDefault(a => a.character == null);
                if (npcActor != null)
                    npcActor.character = c;
            });
            RoomState = RoomState.Matchmaking;

            //hub.Clients.Caller.gotRoomState(RoomState.Matchmaking);
            Sync();
        }

        public void Update(IHubContext hub)
        {
            _updateHub = hub;

            if (RoomState == RoomState.Playing)
            {
                duration -= MyHub.Elapsed;
                if (duration < 0)
                    GotoNextDay();
            }
        }

        /*void CallPlayer(Player player, Action<dynamic> action)
        {
            var client = _updateHub.Clients.Client(player.connectionId);
            action(client);
        }*/

        void CallPlayers(Action<dynamic> action)
        {
            _characters.ForEach(c =>
            {
                var client = _updateHub.Clients.Client(c.Player.connectionId);
                action(client);
            });
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
            /*CallPlayers(c =>
            {
                var actors = _actors.Select(a => new ActorInfo(a)).ToList();
                c.gotRoomState(RoomState);
                c.gotActors(actors);
            });*/
        }
    }
}
