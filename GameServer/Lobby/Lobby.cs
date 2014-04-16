using ApiScheme.Scheme;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    partial class Lobby
    {
        DateTime _bootTime = DateTime.UtcNow;
        DateTime _lastUpdate = DateTime.UtcNow;
        double _elapsed = 0;

        Dictionary<string, Player> _players = new Dictionary<string, Player>();
        Dictionary<string, Player> _playersInLobby = new Dictionary<string, Player>();
        Dictionary<string, Player> _playersInGame = new Dictionary<string, Player>();
        List<Room> _rooms = new List<Room>();
        List<LobbyMessage> _messages = new List<LobbyMessage>();
        ConcurrentQueue<LobbyCommand.Base> _queue = new ConcurrentQueue<LobbyCommand.Base>();
        ConcurrentQueue<RoomCommand.Base> _queueToRoom = new ConcurrentQueue<RoomCommand.Base>();

        List<GetBlacklistOut> _blacklists = new List<GetBlacklistOut>();
        int _nexBlacklistPage = 0;
        double _durationUntilNextPage = 0;

        public Player GetPlayer(string connectionId)
        {
            try
            {
                return _players[connectionId];
            }
            catch
            {
                return null;
            }
        }

        public void Enqueue(LobbyCommand.Base command)
        {
            Console.WriteLine(command.GetType().FullName);
            _queue.Enqueue(command);
        }

        public void EnqueueRoom(RoomCommand.Base command)
        {
            _queueToRoom.Enqueue(command);
        }

        public void Update()
        {
            var now = DateTime.UtcNow;
            _elapsed = (now - _lastUpdate).TotalSeconds;
            _lastUpdate = now;

            // Updates Hub
            ProcessQueue();

            /*// Blacklist
            _durationUntilNextPage -= Elapsed;
            if (_durationUntilNextPage < 0)
            {
                Console.WriteLine("Getting page " + _nexBlacklistPage);
                var blacklist = Api.Get<GetBlacklistOut>(new GetBlacklistIn() { page = _nexBlacklistPage });
                if (_blacklists.Count > _nexBlacklistPage)
                    _blacklists[_nexBlacklistPage] = blacklist;
                else
                    _blacklists.Add(blacklist);
                _nexBlacklistPage++;

                var str = "";
                _blacklists.ForEach(b => b.infos.ForEach(info => str += info.userId + ","));
                Console.WriteLine("CurrentBlacklist: " + str);

                blacklist.infos.ForEach(info =>
                {
                    Kick(info.userId);
                });

                if (blacklist.infos.Count == 0)
                    _nexBlacklistPage = 0;
                _durationUntilNextPage = 10;
            }*/

            // Updates Rooms
            _rooms.ForEach(r => r.Update(_elapsed));

            // Cleans Rooms
            _rooms.RemoveAll(r => r.ShouldBeDeleted);
        }
    }
}
