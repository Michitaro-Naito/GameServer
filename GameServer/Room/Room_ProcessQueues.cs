using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public partial class Room
    {
        void ProcessMessages()
        {
            if (_messagesWillBeApplied.Count == 0)
                return;

            _messages.AddRange(_messagesWillBeApplied);
            _characters.ForEach(c =>
            {
                _updateHub.Clients.Client(c.Player.connectionId).gotRoomMessages(_messagesWillBeApplied.Select(m=>new RoomMessageInfo(m)).ToList());
            });
            _messagesWillBeApplied.Clear();
        }

        /// <summary>
        /// Handles queues. Thread-unsafe.
        /// </summary>
        void ProcessQueues()
        {
            RoomCommand.Base commandBase;
            while (_queue.TryDequeue(out commandBase))
            {
                var type = commandBase.GetType();

                // ----- Adds Character -----
                if (type == typeof(RoomCommand.AddCharacter))
                {
                    var command = (RoomCommand.AddCharacter)commandBase;
                    var client = _updateHub.Clients.Client(command.Player.connectionId);
                    if (_characters.Count > 0 && _characters.Count >= conf.max)
                    {
                        client.addMessage("Could not join. Room is full.");
                        continue;
                    }
                    _characters.Add(command.Character);
                    var npc = _actors.Where(a => a.IsNPC).FirstOrDefault();
                    if (npc != null)
                        npc.character = command.Character;
                    AddActorsForCharacters();
                    /*var actor = _actors.Where(a => a.character == null).RandomElement();
                    if (actor != null)
                    {
                        actor.character = command.Character;
                        SystemMessageAll(string.Format("{0} joined as {1}", _characters, actor));
                    }*/
                    client.addMessage("Joined.");
                    client.broughtTo(ClientState.Playing);

                    // Character added. Shares this information later.
                    _needSync = true;
                }

                // ----- Removes Player -----
                if (type == typeof(RoomCommand.RemovePlayer))
                {
                    var command = (RoomCommand.RemovePlayer)commandBase;
                    var client = _updateHub.Clients.Client(command.Target.connectionId);
                    _characters.RemoveAll(c => c.Player == command.Target);
                    /*_actors.RemoveAll(a =>
                    {
                        return a.character != null && a.character.Player == command.Target;
                    });*/
                    _actors.Where(a=>a.IsOwnedBy(command.Target)).ToList().ForEach(a=>a.character = null);

                    // Brings removed Player to Rooms scene.
                    client.broughtTo(ClientState.Rooms);

                    // Character removed. Shares this information later.
                    _needSync = true;
                }

                // ----- Configures Room -----
                if (type == typeof(RoomCommand.Configure))
                {
                    var command = (RoomCommand.Configure)commandBase;
                    var client = _updateHub.Clients.Client(command.Player.connectionId);

                    if (RoomState != RoomState.Configuring)
                    {
                        client.addMessage("Already configured.");
                        continue;
                    }

                    // Validates Configuration

                    // Applies Configutation
                    conf = command.Configuration;

                    // Initializes Matchmaking
                    AddActorsForCharacters();
                    RoomState = RoomState.Matchmaking;

                    client.addMessage("You have called Configure");
                    _needSync = true;
                }

                // ----- Starts Room -----
                if (type == typeof(RoomCommand.Start))
                {
                    var command = (RoomCommand.Start)commandBase;
                    var client = _updateHub.Clients.Client(command.Player.connectionId);

                    if (RoomState != RoomState.Matchmaking)
                    {
                        client.addMessage("Room can be started only when matchmaking.");
                        continue;
                    }

                    var min = 7;
                    var count = Math.Max(min, _characters.Count);

                    // Adds Actors
                    while (_actors.Count < count)
                        _actors.Add(Actor.CreateUnique(_actors));

                    // Remove NPCs
                    while (_actors.Where(a => a.IsNPC).Count() > 0 && _actors.Count > min)
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
                    duration = conf.interval;

                    client.addMessage("Game started.");
                    _needSync = true;
                }

                // ----- Sends Message -----
                if (type == typeof(RoomCommand.Send))
                {
                    var command = (RoomCommand.Send)commandBase;
                    var client = _updateHub.Clients.Client(command.Player.connectionId);

                    var from = _actors.FirstOrDefault(a => a.character != null && a.character.Player == command.Player);
                    var to = _actors.FirstOrDefault(a => a.id == command.ActorId);
                    _messagesWillBeApplied.Add(new RoomMessage()
                    {
                        id = _nextMessageId++,
                        callerUserId = command.Player.userId,
                        mode = RoomMessage.Mode.All,
                        from = from,
                        to = to,
                        body = command.Message
                    });
                }
            }
        }
    }
}
