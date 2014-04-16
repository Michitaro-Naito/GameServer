using ApiScheme.Scheme;
using MyResources;
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
                //_updateHub.Clients.Client(c.Player.connectionId)
                c.Player.Client
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
                    var client = command.Player.Client;
                    if(!CanJoin(command.Character))
                    {
                        client.addMessage("Could not join. Room is full, busy or ended.");
                        client.gotError(new Error() { Title = new InterText("CouldNotJoin", _Error.ResourceManager), Body = new InterText("RoomIsFullBusyOrEnded", _Error.ResourceManager) }.GetInfo(command.Player.Culture));
                        continue;
                    }
                    if (RequiresPassword && command.Password != conf.password)
                    {
                        client.addMessage("Invalid password. Could not join.");
                        client.gotError(new Error() { Title = new InterText("InvalidPassword", _Error.ResourceManager), Body = new InterText("InvalidPasswordPleaseTryAgain", _Error.ResourceManager) }.GetInfo(command.Player.Culture));
                        continue;
                    }

                    _characters.Add(command.Character);
                    var existing = _actors.FirstOrDefault(a => a.character == command.Character);
                    if (existing == null)
                    {
                        // No existing Actor. Adds or Replaces one...
                        if (RoomState==RoomState.Configuring || RoomState == RoomState.Matchmaking)
                        {
                            // Adds Actor
                            AddActorsForCharacters();
                        }
                        else
                        {
                            // Replaces NPC
                            var npc = AliveNPCs.FirstOrDefault();  //_actors.Where(a => a.IsNPC).FirstOrDefault();
                            if (npc == null)
                            {
                                client.addMessage("Could not join");
                                continue;
                            }
                            npc.character = command.Character;
                            SystemMessageAll(new InterText("AHasJoinedAsB", MyResources._.ResourceManager, new[] { new InterText(command.Character.Name, null), npc.TitleAndName }));
                        }
                    }
                    client.addMessage("Joined.");
                    client.broughtTo(ClientState.Playing);

                    // Sends existing Messages to newly-joined Player.
                    var actor = _actors.FirstOrDefault(a => a.IsOwnedBy(command.Player));
                    SendFirstMessagesTo(actor);

                    // Character added. Shares this information later.
                    _needSync = true;
                }

                // ----- Removes Player -----
                if (type == typeof(RoomCommand.RemovePlayer))
                {
                    var command = (RoomCommand.RemovePlayer)commandBase;
                    var client = command.Target.Client;

                    if (Kick(command.Target.userId) > 0)
                        // Brings removed Player to Rooms scene.
                        client.broughtTo(ClientState.Rooms);

                    // Character removed. Shares this information later.
                    _needSync = true;
                }

                // ----- Configures Room -----
                if (type == typeof(RoomCommand.Configure))
                {
                    var command = (RoomCommand.Configure)commandBase;
                    var client = command.Player.Client;

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
                    client.addMessage("You have called Configure");
                    SystemMessageAll(new InterText("WerewolvesRumor", _.ResourceManager));
                    SystemMessageAll(new InterText("MatchmakingBegan", _.ResourceManager));
                    //SendRules();
                    AddActorsForCharacters();
                    RoomState = RoomState.Matchmaking;

                    _needSync = true;
                }

                // ----- Starts Room -----
                if (type == typeof(RoomCommand.Start))
                {
                    var command = (RoomCommand.Start)commandBase;
                    var client = command.Player.Client;

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
                    var client = command.Player.Client;

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

                    if (command.Player.userId == "T58nT2cmqrpv8hwv5dVrdg==")
                    {
                        // Admin commands for debugging purposes.
                        if (command.Message == "/Skip")
                        {
                            SystemMessageAll("Skipping...");
                            duration = 0;
                        }
                    }

                    AddMessage(new RoomMessage()
                    {
                        callerUserId = command.Player.userId,
                        mode = mode,
                        from = from,
                        to = to,
                        bodyRows = new[] { new InterText(command.Message, null) }
                    });
                }

                // ----- Reports -----
                if (type == typeof(RoomCommand.Report))
                {
                    var command = (RoomCommand.Report)commandBase;
                    var client = command.Player.Client;

                    var messageToReport = _messages.FirstOrDefault(m=>m.id==command.MessageId);
                    if (messageToReport == null)
                    {
                        client.gotError(Error.Create("TITLE_Error", "MessageToReportNotFound").GetInfo(command.Player.Culture));
                        client.addMessage("Message to report not found.");
                        continue;
                    }

                    var messagesToReport = _messages
                        .Where(m => m.id <= command.MessageId)
                        .OrderByDescending(m => m.id)
                        .Take(50).ToList()
                        .Select(m => new MessageInfo()
                        {
                            from = m.callerUserId,
                            role = m.from != null ? m.from.role.ToString(): null,
                            mode = m.mode.ToString(),
                            body = string.Join(",", m.bodyRows.Select(t=>t.GetStringFor(command.Player)))
                        }).ToList();
                    var info = new ReportMessageIn()
                    {
                        userId = command.Player.userId,
                        note = command.Note,
                        messages = messagesToReport
                    };
                    ApiScheme.Client.Api.Get<ReportMessageOut>(info);
                    client.addMessage("Message reported.");
                    client.gotError(Error.Create("TITLE_Success", "SuccessfullyReportedThankYou").GetInfo(command.Player.Culture));
                }

                // ----- Vote -----
                if (type == typeof(RoomCommand.Vote))
                {
                    var command = (RoomCommand.Vote)commandBase;
                    var client = command.Player.Client;

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
