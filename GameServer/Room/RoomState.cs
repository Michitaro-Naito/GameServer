using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public enum RoomState
    {
        Configuring = 0000,
        Matchmaking = 0001,
        Playing = 0002,
        Ending = 0003,
        Ended = 0004
    }
}
