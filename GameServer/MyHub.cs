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

namespace GameServer {
    public class MyHub : Hub {
        // ----- Static Variable -----
        static Lobby _lobby = new Lobby();



        // ----- Property -----

        /// <summary>
        /// Returns current Player.
        /// </summary>
        public Player Player {
            get {
                // KeyNotFoundException
                //return _players[Context.ConnectionId];
                return _lobby.GetPlayer(Context.ConnectionId);
            }
        }



        // ----- Override -----

        public override Task OnConnected() {
            Enqueue(new LobbyCommand.OnConnected() { Client = Clients.Caller });
            return base.OnConnected();
        }

        public override Task OnReconnected() {
            Enqueue(new LobbyCommand.OnReconnected() { });
            return base.OnReconnected();
        }

        public override Task OnDisconnected() {
            Enqueue(new LobbyCommand.OnDisconnected() { });
            return base.OnDisconnected();
        }



        // ----- Static Method -----
        public static void Update() {
            _lobby.Update();
        }

        public void Enqueue(LobbyCommand.Base command) {
            command.ConnectionId = Context.ConnectionId;
            _lobby.Enqueue(command);
        }

        public void EnqueueRoom(RoomCommand.Base command) {
            command.ConnectionId = Context.ConnectionId;
            _lobby.EnqueueRoom(command);
        }



        // ----- Method ( Client to Server ) -----

        /// <summary>
        /// Authenticates Player using GamePass.
        /// </summary>
        /// <param name="gamePass"></param>
        public void Authenticate(string culture, string passString) {
            Enqueue(new LobbyCommand.Authenticate() { Culture = culture, PassString = passString });
        }

        public void GetCharacters() {
            Enqueue(new LobbyCommand.GetCharacters() { });
        }

        public void CreateCharacter(string modelName, string name) {
            Enqueue(new LobbyCommand.CreateCharacter() { ModelName = modelName, Name = name });
        }

        public void SelectCharacter(string name) {
            Enqueue(new LobbyCommand.SelectCharacter() { Name = name });
        }

        public void GetLobbyMessages() {
            Enqueue(new LobbyCommand.GetLobbyMessages() { });
        }

        public void GetRooms() {
            Enqueue(new LobbyCommand.GetRooms() { });
        }

        /// <summary>
        /// Creates a Room and join.
        /// </summary>
        public void CreateRoom() {
            Enqueue(new LobbyCommand.CreateRoom() { });
        }

        public void JoinRoom(int roomId, string password) {
            Enqueue(new LobbyCommand.JoinRoom() { RoomId = roomId, Password = password });
        }

        public void LobbySend(string message) {
            Enqueue(new LobbyCommand.LobbySend() { Message = message });
        }

        public void RoomSend(int roomSendMode, int actorId, string message) {
            /*var room = Room;
            if (room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            room.Queue(new RoomCommand.Send(Player, roomSendMode, actorId, message));*/
            EnqueueRoom(new RoomCommand.Send() { RoomSendMode = roomSendMode, ActorId = actorId, Message = message });
        }

        public void RoomReportMessage(int messageId, string note) {
            /*if (Room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            SystemMessage("Queueing to report..." + messageId + note);
            Room.Queue(new RoomCommand.Report(Player, messageId, note));*/
            EnqueueRoom(new RoomCommand.Report() { MessageId = messageId, Note = note });
        }

        public void RoomConfigure(Room.ClientConfiguration conf) {
            /*var room = Room;
            if (room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            room.Queue(new RoomCommand.Configure(Player, conf));*/
            EnqueueRoom(new RoomCommand.Configure() { Configuration = conf });
        }

        public void RoomStart() {
            /*var room = Room;
            if (room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            room.Queue(new RoomCommand.Start(Player));*/
            EnqueueRoom(new RoomCommand.Start() { });
        }

        public void RoomVote(int executionId, int attackId, int fortuneTellId, int guardId) {
            /*var room = Room;
            if (room == null)
            {
                SystemMessage("You are not in Room.");
                return;
            }
            SystemMessage("Voting..." + executionId);
            room.Queue(new RoomCommand.Vote(Player, executionId, attackId, fortuneTellId, guardId));*/
            EnqueueRoom(new RoomCommand.Vote() {
                ExecutionId = executionId,
                AttackId = attackId,
                FortuneTellId = fortuneTellId,
                GuardId = guardId
            });
        }

        public void QuitRoom() {
            /*var room = Room;
            SystemMessage(string.Format("Quitting from {0}", room));
            room.Queue(new RoomCommand.RemovePlayer(Player, Player));*/
            EnqueueRoom(new RoomCommand.RemovePlayer(){ });
        }

    }
}
