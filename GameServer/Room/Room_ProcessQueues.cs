using ApiScheme.Scheme;
using MyResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            //_characters.ForEach(c =>
            CharactersAndSpectators.ToList().ForEach(c =>
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
                try {
                    var type = commandBase.GetType();
                    GetType().GetMethod("PQ_" + type.Name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, new object[] { commandBase });
                }
                catch (Exception e) {
                    Logger.WriteLine(e.ToString());
                }
            }
        }

        /// <summary>
        /// Adds Character to Room.
        /// (Player comes.)
        /// </summary>
        /// <param name="command"></param>
        void PQ_AddCharacter(RoomCommand.AddCharacter command) {
            var client = command.Sender.Client;
            if (!CanJoin(command.Character)) {
                client.addMessage("Could not join. Room is full, busy or ended.");
                client.gotError(new Error() { Title = new InterText("CouldNotJoin", _Error.ResourceManager), Body = new InterText("RoomIsFullBusyOrEnded", _Error.ResourceManager) }.GetInfo(command.Sender.Culture));
                return;
            }
            if (RequiresPassword && command.Password != conf.password) {
                client.addMessage("Invalid password. Could not join.");
                client.gotError(new Error() { Title = new InterText("InvalidPassword", _Error.ResourceManager), Body = new InterText("InvalidPasswordPleaseTryAgain", _Error.ResourceManager) }.GetInfo(command.Sender.Culture));
                return;
            }

            var existing = _actors.FirstOrDefault(a => a.character == command.Character);
            if (existing != null) {
                // Replaces existing Actor (Player is coming back.)
                _characters.Add(command.Character);
                command.Character.Room = this;
                existing.character = command.Character;
            }
            else {
                var npc = AliveNPCs.FirstOrDefault();
                if (npc != null) {
                    // Replaces NPC (Players is a new comer.)
                    _characters.Add(command.Character);
                    command.Character.Room = this;
                    npc.character = command.Character;
                    SystemMessageAll(new InterText("AHasJoinedAsB", MyResources._.ResourceManager, new[] { new InterText(command.Character.Name, null), npc.TitleAndName }));
                }
                else {
                    if (RoomState == RoomState.Configuring || RoomState == RoomState.Matchmaking) {
                        // Adds Actor (Player is a new comer.)
                        _characters.Add(command.Character);
                        command.Character.Room = this;
                        AddActorsForCharacters();
                    }
                    else {
                        // Failed
                        client.addMessage("Could not join");
                        return;
                    }
                }
            }
            client.addMessage("Joined.");
            client.broughtTo(ClientState.Playing);

            // Sends existing Messages to newly-joined Player.
            var actor = _actors.FirstOrDefault(a => a.IsOwnedBy(command.Sender));
            SendFirstMessagesTo(actor);

            // Notifies Lobby
            EnqueueLobby(new LobbyCommand.PlayerJoinedRoom() { ConnectionId = command.ConnectionId, Sender = command.Sender });

            // Character added. Shares this information later.
            _needSync = true;
        }

        /// <summary>
        /// Adds Character as a Spectator.
        /// (Player comes to Spectate.)
        /// </summary>
        /// <param name="command"></param>
        void PQ_SpectateCharacter(RoomCommand.SpectateCharacter command) {
            _spectators.Add(command.Character);
            command.Character.Room = this;
            command.Character.Player.BroughtTo(ClientState.Playing);
            SendFirstMessagesTo(command.Character);
            EnqueueLobby(new LobbyCommand.PlayerJoinedRoom() { ConnectionId = command.ConnectionId, Sender = command.Sender });

            _needSync = true;
        }

        /// <summary>
        /// Removes Player from Room.
        /// (Player has gone.)
        /// </summary>
        /// <param name="command"></param>
        void PQ_RemovePlayer(RoomCommand.RemovePlayer command) {
            var client = command.Sender.Client;

            /*if (Kick(command.Sender.userId) > 0)
                // Brings removed Player to Rooms scene.
                client.broughtTo(ClientState.Rooms);*/
            Kick(command.Sender.userId);

            // Character removed. Shares this information later.
            _needSync = true;
        }

        /// <summary>
        /// Player configures Room.
        /// </summary>
        /// <param name="command"></param>
        void PQ_Configure(RoomCommand.Configure command) {
            var client = command.Sender.Client;

            if (RoomState != RoomState.Configuring) {
                client.addMessage("Already configured.");
                return;
            }

            // Validates Configuration
            var result = command.Configuration.Validate();
            if (!result.Success) {
                client.addMessage("Validation failed.");
                result.Errors.ForEach(e => client.addMessage(e.GetString(command.Sender.Culture)));
                client.gotValidationErrors(command.Configuration.ModelName, result.Errors.Select(e => e.GetStringFor(command.Sender)));
                return;
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

        /// <summary>
        /// Player starts Room.
        /// </summary>
        /// <param name="command"></param>
        void PQ_Start(RoomCommand.Start command) {
            var client = command.Sender.Client;

            if (RoomState != RoomState.Matchmaking || duration > 0) {
                client.addMessage("Room can be started only when matchmaking.");
                return;
            }

            var actor = _actors.FirstOrDefault(a => a.IsOwnedBy(command.Sender));
            if (actor == null || !IsRoomMaster(actor)) {
                client.addMessage("Only the RoomMaster can start.");
                return;
            }

            if (command.Roles != null) {
                command.Roles.ForEach(r => Console.WriteLine(r.id.ToString() + r.amount.ToString()));
            }

            if(CastRoles(command.Roles))
                CountDownToStart();
        }

        /// <summary>
        /// Player sends Message.
        /// </summary>
        /// <param name="command"></param>
        void PQ_Send(RoomCommand.Send command) {
            var client = command.Sender.Client;

            var from = _actors.FirstOrDefault(a => a.character != null && a.character.Player == command.Sender);
            if (from == null) {
                SystemMessageAll("from must not be null.");
                return;
            }

            var mode = (RoomMessage.Mode)command.RoomSendMode;
            if (!ModesFor(from).Contains(mode)) {
                SystemMessageAll("Mode not allowed.");
                return;
            }

            var to = _actors.FirstOrDefault(a => a.id == command.ActorId);
            if (mode == RoomMessage.Mode.Private && to == null) {
                SystemMessageAll("to must not be null for private messages.");
                return;
            }

            if (!new[] { RoomState.Matchmaking, RoomState.Playing, RoomState.Ending }.Contains(RoomState)) {
                SystemMessageAll("You can chat only when matchmaking, playing, ending");
                return;
            }

            if (command.Sender.userId == "T58nT2cmqrpv8hwv5dVrdg==") {
                // Admin commands for debugging purposes.
                if (command.Message == "/Skip") {
                    SystemMessageAll("Skipping...");
                    duration = 0;
                }
            }

            var bodyRows = command.Message.Split(new char[]{'\n'}, 15).Select(str=>new InterText(str, null)).ToArray();

            AddMessage(new RoomMessage() {
                callerUserId = command.Sender.userId,
                mode = mode,
                from = from,
                to = to,
                //bodyRows = new[] { new InterText(command.Message, null) }
                bodyRows = bodyRows
            });
        }

        /// <summary>
        /// Player reports Messages as maluse.
        /// </summary>
        /// <param name="command"></param>
        void PQ_Report(RoomCommand.Report command) {
            var client = command.Sender.Client;

            var messageToReport = _messages.FirstOrDefault(m => m.id == command.MessageId);
            if (messageToReport == null) {
                client.gotError(Error.Create("TITLE_Error", "MessageToReportNotFound").GetInfo(command.Sender.Culture));
                client.addMessage("Message to report not found.");
                return;
            }

            var messagesToReport = _messages
                .Where(m => m.id <= command.MessageId)
                .OrderByDescending(m => m.id)
                .Take(50).ToList()
                .Select(m => new MessageInfo() {
                    from = m.callerUserId,
                    role = m.from != null ? m.from.role.ToString() : null,
                    mode = m.mode.ToString(),
                    body = string.Join(",", m.bodyRows.Select(t => t.GetStringFor(command.Sender)))
                }).ToList();
            var info = new ReportMessageIn() {
                userId = command.Sender.userId,
                note = command.Note,
                messages = messagesToReport
            };
            ApiScheme.Client.Api.Get<ReportMessageOut>(info);
            client.addMessage("Message reported.");
            client.gotError(Error.Create("TITLE_Success", "SuccessfullyReportedThankYou").GetInfo(command.Sender.Culture));
        }

        /// <summary>
        /// Player votes.
        /// </summary>
        /// <param name="command"></param>
        void PQ_Vote(RoomCommand.Vote command) {
            var client = command.Sender.Client;

            var actor = _actors.FirstOrDefault(a => a.IsOwnedBy(command.Sender));
            if (actor == null) {
                client.addMessage("Voting... but your actor not found.");
                return;
            }

            actor.ActorToExecute = _actors.FirstOrDefault(a => a.id == command.ExecutionId);
            actor.ActorToAttack = _actors.FirstOrDefault(a => a.id == command.AttackId);
            actor.ActorToFortuneTell = _actors.FirstOrDefault(a => a.id == command.FortuneTellId);
            actor.ActorToGuard = _actors.FirstOrDefault(a => a.id == command.GuardId);
            /*client.addMessage("Voted. ActorToExecute is: " + actor.ActorToExecute);
            client.addMessage("Voted. ActorToAttack is: " + actor.ActorToAttack);
            client.addMessage("Voted. ActorToFortuneTell is: " + actor.ActorToFortuneTell);
            client.addMessage("Voted. ActorToGuard is: " + actor.ActorToGuard);*/
        }

        void PQ_Skip(RoomCommand.Skip command) {
            //if (command.Sender == null || !IsRoomMaster(command.Sender.Character))
            //    return;
            if (command.Sender == null)
                // Sent by Unknown?
                return;

            if (!IsRoomMaster(command.Sender.Character))
                // Not RoomMaster.
                return;

            if (!new[] { RoomState.Playing, RoomState.Ending }.Contains(RoomState))
                // Only available Playing or Ending.
                return;

            SystemMessageAll(InterText.Create("SkippedByRoomMasterA", _.ResourceManager, command.Sender.Character));
            duration = 0;
        }

        void PQ_Kick(RoomCommand.Kick command) {
            if (command.Sender == null)
                // Sent by Unknown?
                return;

            if (!IsRoomMaster(command.Sender.Character))
                // Not RoomMaster.
                return;

            if (!new[] { RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState))
                // Only available Matchmaking or Playing.
                return;

            if (command.CharacterName == null || command.CharacterName == "")
                // Empty
                return;

            if (command.CharacterName == "test") {
                // Kicking Admin.
                SystemMessageAll(InterText.Create("PleaseDoNotKickTest", _Error.ResourceManager));
                return;
            }

            // OK. Kicks.
            var characterToKick = _characters.FirstOrDefault(c => c.Name == command.CharacterName);
            if (characterToKick != null && characterToKick.Player != null) {
                SystemMessageAll(InterText.Create("RoomMasterAKickedB", _.ResourceManager, command.Sender.Character, characterToKick.Name));
                Kick(characterToKick.Player.userId);
            }
        }

        void PQ_Kill(RoomCommand.Kill command) {
            if (command.Sender == null)
                // Sent by Unknown?
                return;

            if (!IsRoomMaster(command.Sender.Character))
                // Not RoomMaster.
                return;

            if (!new[] { RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState))
                // Only available Matchmaking or Playing.
                return;

            var actorToKill = AliveActors.FirstOrDefault(a => a.id == command.ActorId);
            if (actorToKill != null) {
                actorToKill.IsDead = true;
                SystemMessageAll(InterText.Create("AHasBeenKilledByRoomMasterB", _.ResourceManager, actorToKill.TitleAndName, command.Sender.Character.Name));
                _needSync = true;
            }
        }

        void PQ_Revive(RoomCommand.Revive command) {
            if (command.Sender == null)
                // Sent by Unknown?
                return;

            if (!IsRoomMaster(command.Sender.Character))
                // Not RoomMaster.
                return;

            if (!new[] { RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState))
                // Only available Matchmaking or Playing.
                return;

            var actorToRevive = DeadActors.FirstOrDefault(a => a.id == command.ActorId);
            if (actorToRevive != null) {
                actorToRevive.IsDead = false;
                SystemMessageAll(InterText.Create("AHasBeenRevivedByRoomMasterB", _.ResourceManager, actorToRevive.TitleAndName, command.Sender.Character.Name));
                _needSync = true;
            }
        }

        void PQ_SetRole(RoomCommand.SetRole command) {
            if (command.Sender == null)
                // Sent by Unknown?
                return;

            if (!IsRoomMaster(command.Sender.Character))
                // Not RoomMaster.
                return;

            if (!new[] { RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState))
                // Only available Matchmaking or Playing.
                return;

            var actor = _actors.FirstOrDefault(a => a.id == command.ActorId);
            if (actor != null) {
                actor.role = command.Role;
                SystemMessageAll(InterText.Create("RoomMasterASetRoleOfSomebody", _.ResourceManager, command.Sender.Character));
                _needSync = true;
            }
        }

    }
}
