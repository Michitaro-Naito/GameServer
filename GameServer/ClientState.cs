using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public enum ClientState : int
    {
        Disconnected        = 0000,

        Characters          = 1000,
        CreateCharacters    = 1001,

        Rooms               = 2000,

        Playing             = 3000
    }
}
