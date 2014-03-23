using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public partial class Room
    {
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
                    var actor = _actors.Where(a => a.character == null).RandomElement();
                    if (actor != null)
                    {
                        actor.character = command.Character;
                        SystemMessageAll(string.Format("{0} joined as {1}", _characters, actor));
                    }
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
                    _actors.RemoveAll(a =>
                    {
                        return a.character != null && a.character.Player == command.Target;
                    });

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
                    _actors = Actor.Create(conf.max);
                    _characters.ForEach(c =>
                    {
                        var npcActor = _actors.FirstOrDefault(a => a.character == null);
                        if (npcActor != null)
                            npcActor.character = c;
                    });
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
                    duration = conf.interval;

                    client.addMessage("Game started.");
                    _needSync = true;
                }
            }
        }
    }
}
