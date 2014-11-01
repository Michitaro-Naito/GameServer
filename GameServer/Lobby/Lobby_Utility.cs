using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    partial class Lobby
    {
        /// <summary>
        /// Kicks Player from this server.
        /// </summary>
        /// <param name="userId"></param>
        void Kick(string userId)
        {
            // Kicks from Room
            _rooms.ForEach(r => r.Kick(userId, false, false));

            // Kicks from Lobby
            var keysToRemove = new List<string>();
            _players.Where(en => en.Value.userId == userId).ToList().ForEach(en =>
            {
                keysToRemove.Add(en.Key);
                en.Value.Client.gotDisconnectionRequest("ゲームから切断されました。別の画面を開いたり端末をスリープモードにすると発生することがあります。");
            });
            keysToRemove.ForEach(key =>
            {
                _players.Remove(key);
                _playersInLobby.Remove(key);
                _playersInGame.Remove(key);
            });
        }

        void BringPlayerToRoom(Player p, int roomId, string password)
        {
            var room = _rooms.FirstOrDefault(r => r.roomId == roomId);
            if (room == null)
            {
                p.GotSystemMessage("Room not found:" + roomId);
                return;
            }

            if (p.Character == null)
            {
                p.GotSystemMessage("Failded to join Room. Character not selected.");
                return;
            }

            _rooms.ForEach(r => r.Queue(new RoomCommand.RemovePlayer(){ ConnectionId = p.connectionId, Sender = GetPlayer(p.connectionId) }));
            room.Queue(new RoomCommand.AddCharacter() {
                ConnectionId = p.connectionId,
                Sender = GetPlayer(p.connectionId),
                Character = p.Character,
                Password = password
            });
        }

        void LetPlayerSpectate(Player p, int roomId) {
            var room = _rooms.FirstOrDefault(r => r.roomId == roomId);
            if (room == null) {
                p.GotSystemMessage("Room not found: " + roomId);
                return;
            }
            if (p.Character == null) {
                p.GotSystemMessage("Failded to join Room. Character not selected.");
                return;
            }

            _rooms.ForEach(r => r.Queue(new RoomCommand.RemovePlayer() { ConnectionId = p.connectionId, Sender = GetPlayer(p.connectionId) }));
            room.Queue(new RoomCommand.SpectateCharacter() {
                ConnectionId = p.connectionId,
                Sender = GetPlayer(p.connectionId),
                Character = p.Character
            });
        }
    }
}
