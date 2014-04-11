using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class LobbyMessageInfo
    {
        public DateTime Created;
        public string name;
        public string body;
    }

    class LobbyMessage
    {
        public DateTime Created;
        public string name;
        public InterText body;

        public LobbyMessage()
        {
            Created = DateTime.UtcNow;
            name = "";
        }

        public LobbyMessageInfo ToInfo(Player player)
        {
            return new LobbyMessageInfo()
            {
                Created = Created,
                name = name,
                body = body.GetStringFor(player)
            };
        }
    }
}
