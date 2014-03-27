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
                var actor = _actors.FirstOrDefault(a => a.character == c);
                _updateHub.Clients.Client(c.Player.connectionId)
                    .gotRoomMessages(_messagesWillBeApplied.Where(m=>m.IsVisibleFor(this, actor)).Select(m=>new RoomMessageInfo(m, c.Player.Culture)).ToList());
            });
            _messagesWillBeApplied.Clear();
            while (_messages.Count > 1000)
                _messages.RemoveAt(0);
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
                    if (RequiresPassword && command.Password != conf.password)
                    {
                        client.addMessage("Invalid password. Could not join.");
                        client.addMessage(command.Password);
                        client.addMessage(conf.password);
                        continue;
                    }
                    _characters.Add(command.Character);
                    var npc = _actors.Where(a => a.IsNPC).FirstOrDefault();
                    if (npc != null)
                        npc.character = command.Character;
                    AddActorsForCharacters();
                    client.addMessage("Joined.");
                    client.broughtTo(ClientState.Playing);

                    // Sends Messages to newly-joined Player.
                    var actor = _actors.FirstOrDefault(a => a.IsOwnedBy(command.Player));
                    if (actor != null)
                    {
                        client.gotRoomMessages(
                            _messages
                                .Where(m => m.IsVisibleFor(this, actor))
                                .Select(m=>new RoomMessageInfo(m, command.Player.Culture)),
                            true);
                    }

                    // Character added. Shares this information later.
                    _needSync = true;
                }

                // ----- Removes Player -----
                if (type == typeof(RoomCommand.RemovePlayer))
                {
                    var command = (RoomCommand.RemovePlayer)commandBase;
                    var client = _updateHub.Clients.Client(command.Target.connectionId);
                    _characters.RemoveAll(c => c.Player == command.Target);
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
                    var result = command.Configuration.Validate();
                    if (!result.Success)
                    {
                        client.addMessage("Validation failed.");
                        result.Errors.ForEach(e => client.addMessage(e.GetString(command.Player.Culture)));
                        client.gotValidationErrors(command.Configuration.ModelName, result.Errors.Select(e => e.GetStringFor(command.Player)));
                        continue;
                    }

                    // Applies Configutation
                    conf = command.Configuration.ToConfiguration();

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

                    if (RoomState != RoomState.Matchmaking || duration > 0)
                    {
                        client.addMessage("Room can be started only when matchmaking.");
                        continue;
                    }

                    var actor = _actors.FirstOrDefault(a => a.IsOwnedBy(command.Player));
                    if (actor == null || !IsRoomMaster(actor))
                    {
                        client.addMessage("Only the RoomMaster can start.");
                        continue;
                    }

                    CountDownToStart();
                }

                // ----- Sends Message -----
                if (type == typeof(RoomCommand.Send))
                {
                    var command = (RoomCommand.Send)commandBase;
                    var client = _updateHub.Clients.Client(command.Player.connectionId);

                    var from = _actors.FirstOrDefault(a => a.character != null && a.character.Player == command.Player);
                    if (from == null)
                    {
                        SystemMessageAll("from must not be null.");
                        continue;
                    }

                    var mode = (RoomMessage.Mode)command.RoomSendMode;
                    if (!ModesFor(from).Contains(mode))
                    {
                        SystemMessageAll("Mode not allowed.");
                        continue;
                    }

                    var to = _actors.FirstOrDefault(a => a.id == command.ActorId);
                    if (mode == RoomMessage.Mode.Private && to == null)
                    {
                        SystemMessageAll("to must not be null for private messages.");
                        continue;
                    }

                    if (!new[] { RoomState.Matchmaking, RoomState.Playing, RoomState.Ending }.Contains(RoomState))
                    {
                        SystemMessageAll("You can chat only when matchmaking, playing, ending");
                        continue;
                    }

                    AddMessage(new RoomMessage()
                    {
                        //id = _nextMessageId++,
                        callerUserId = command.Player.userId,
                        mode = mode,
                        from = from,
                        to = to,
                        //body = command.Message
                        bodyRows = new[] { new InterText(command.Message, null) }
                    });
                }

                // ----- Vote -----
                if (type == typeof(RoomCommand.Vote))
                {
                    var command = (RoomCommand.Vote)commandBase;
                    var client = _updateHub.Clients.Client(command.Player.connectionId);

                    var actor = _actors.FirstOrDefault(a => a.IsOwnedBy(command.Player));
                    if (actor == null)
                    {
                        client.addMessage("Voting... but your actor not found.");
                        continue;
                    }

                    actor.ActorToExecute = _actors.FirstOrDefault(a => a.id == command.ExecutionId);
                    actor.ActorToAttack = _actors.FirstOrDefault(a => a.id == command.AttackId);
                    actor.ActorToFortuneTell = _actors.FirstOrDefault(a => a.id == command.FortuneTellId);
                    actor.ActorToGuard = _actors.FirstOrDefault(a => a.id == command.GuardId);
                    client.addMessage("Voted. ActorToExecute is: " + actor.ActorToExecute);
                }
            }
        }
    }
}
