using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class LobbyMessage
    {
        public DateTime Created;
        public string name;
        public string body;

        public LobbyMessage()
        {
            Created = DateTime.UtcNow;
            name = "";
            body = "";
        }
    }
}
