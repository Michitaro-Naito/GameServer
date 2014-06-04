using GameServer.ClientModel;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameServer {

    /// <summary>
    /// Accepts connections from Players.
    /// Just queues everything.
    /// cf. Lobby for GameLogics.
    /// </summary>
    public class MyHub : Hub {

        // ----- Static Variable -----

        static Lobby _lobby = new Lobby();



        // ----- Static Method -----

        public static void Update() {
            _lobby.Update();
        }



        // ----- Override -----

        public override Task OnConnected() {
            Enqueue(new LobbyCommand.OnConnected() { Client = Clients.Caller });
            return base.OnConnected();
        }

        public override Task OnReconnected() {
            Enqueue(new LobbyCommand.OnConnected() { Client = Clients.Caller });
            //Enqueue(new LobbyCommand.OnReconnected() { });
            return base.OnReconnected();
        }

        public override Task OnDisconnected() {
            Enqueue(new LobbyCommand.OnDisconnected() { });
            return base.OnDisconnected();
        }



        // ----- Public Method ( Client to Server ) -----

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

        public void SpectateRoom(int roomId) {
            Enqueue(new LobbyCommand.SpectateRoom() { RoomId = roomId });
        }

        public void LobbySend(string message) {
            if (message.Length > 50)
                return;
            Enqueue(new LobbyCommand.LobbySend() { Message = message });
        }

        public void RoomSend(int roomSendMode, int actorId, string message) {
            EnqueueRoom(new RoomCommand.Send() { RoomSendMode = roomSendMode, ActorId = actorId, Message = message });
        }

        public void RoomReportMessage(int messageId, string note) {
            EnqueueRoom(new RoomCommand.Report() { MessageId = messageId, Note = note });
        }

        public void RoomConfigure(Room.ClientConfiguration conf) {
            EnqueueRoom(new RoomCommand.Configure() { Configuration = conf });
        }

        public void RoomStart() {
            EnqueueRoom(new RoomCommand.Start() { });
        }

        public void RoomStart(List<ClientRoleAmount> roles) {
            EnqueueRoom(new RoomCommand.Start() { Roles = roles });
        }

        public void RoomVote(int executionId, int attackId, int fortuneTellId, int guardId) {
            EnqueueRoom(new RoomCommand.Vote() {
                ExecutionId = executionId,
                AttackId = attackId,
                FortuneTellId = fortuneTellId,
                GuardId = guardId
            });
        }

        public void RoomQuit() {
            EnqueueRoom(new RoomCommand.RemovePlayer(){ });
        }

        public void RoomGetOlderMessages(RoomCommand.GetOlderMessages command) {
            EnqueueRoom(command);
        }

        // ----- RoomMaster -----
        public void RoomSkip() {
            EnqueueRoom(new RoomCommand.Skip() { });
        }

        public void RoomKick(RoomCommand.Kick command) {
            EnqueueRoom(command);
        }

        public void RoomKill(RoomCommand.Kill command) {
            EnqueueRoom(command);
        }

        public void RoomRevive(RoomCommand.Revive command) {
            EnqueueRoom(command);
        }

        public void RoomSetRole(RoomCommand.SetRole command) {
            EnqueueRoom(command);
        }



        // ----- Private Method -----

        void Enqueue(LobbyCommand.Base command) {
            command.ConnectionId = Context.ConnectionId;
            _lobby.Enqueue(command);
        }

        void EnqueueRoom(RoomCommand.Base command) {
            command.ConnectionId = Context.ConnectionId;
            _lobby.EnqueueRoom(command);
        }

    }

}
