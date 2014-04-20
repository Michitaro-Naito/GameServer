using ApiScheme.Client;
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

        double _durationUntilNextStatusReport = 0;
        int _maxPlayers = 3;
        int _frames = 0;
        double _statusReportIntervalSeconds = 10;
        double _maxElapsedSeconds = 0;

        public Lobby() {
            _maxPlayers = GameConfiguration.MaxPlayers;
        }

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

            // Reports Status
            if(GameConfiguration.ReportStatus)
                ReportGameServerStatus(_elapsed);

            // Blacklist
            GetBlacklist(_elapsed);

            // Updates Rooms
            var roomsToRemove = new List<Room>();
            _rooms.ForEach(r => {
                try {
                    r.Update(_elapsed);
                }
                catch (Exception e) {
                    roomsToRemove.Add(r);
                    Logger.WriteLine("Room terminated because Update() thrown an exception. : " + e.ToString());
                }
            });
            _rooms.RemoveAll(r=>roomsToRemove.Contains(r));

            // Cleans Rooms
            _rooms.RemoveAll(r => r.ShouldBeDeleted);
        }

        /// <summary>
        /// Reports GameServerStatus to ApiServer.
        /// </summary>
        /// <param name="elapsed"></param>
        void ReportGameServerStatus(double elapsed) {
            _maxElapsedSeconds = Math.Max(_maxElapsedSeconds, elapsed);
            _durationUntilNextStatusReport -= elapsed;
            _frames++;
            if (_durationUntilNextStatusReport > 0)
                return;

            Console.WriteLine("Reporting...");
            var framesPerInterval = _frames;
            _frames = 0;
            var maxElapsedSeconds = _maxElapsedSeconds;
            _maxElapsedSeconds = 0;
            _durationUntilNextStatusReport = _statusReportIntervalSeconds;

            try {
                var o = ApiScheme.Client.Api.Get<ReportGameServerStatusOut>(new ReportGameServerStatusIn() {
                    status = new GameServerStatus() {
                        host = GameConfiguration.Host,
                        port = GameConfiguration.Port,
                        name = GameConfiguration.Name,
                        players = _players.Count,
                        maxPlayers = _maxPlayers,
                        framesPerInterval = framesPerInterval,
                        reportIntervalSeconds = _statusReportIntervalSeconds,
                        maxElapsedSeconds = maxElapsedSeconds
                    }
                });
            }
            catch (Exception e) {
                Logger.WriteLine(e.ToString());
            }

            Console.WriteLine("Reported");
        }

        void GetBlacklist(double elapsed) {
            _durationUntilNextPage -= elapsed;
            if (_durationUntilNextPage < 0) {
                _durationUntilNextPage = 10;
                Console.WriteLine("Getting page " + _nexBlacklistPage);

                try {
                    var blacklist = Api.Get<GetBlacklistOut>(new GetBlacklistIn() { page = _nexBlacklistPage });
                    if (_blacklists.Count > _nexBlacklistPage)
                        _blacklists[_nexBlacklistPage] = blacklist;
                    else
                        _blacklists.Add(blacklist);
                    _nexBlacklistPage++;

                    var str = "";
                    _blacklists.ForEach(b => b.infos.ForEach(info => str += info.userId + ","));
                    Console.WriteLine("CurrentBlacklist: " + str);

                    blacklist.infos.ForEach(info => {
                        Kick(info.userId);
                    });

                    if (blacklist.infos.Count == 0)
                        _nexBlacklistPage = 0;
                }
                catch (Exception e){
                    Logger.WriteLine(e.ToString());
                }
            }
        }
    }
}
