using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class RoomInfo
    {
        public int roomId;
        public string guid;
        public string name;
        public int max;
        public int interval;
        public bool requiresPassword;

        public RoomInfo(Room room)
        {
            roomId = room.roomId;
            guid = room.guid;
            name = room.conf.name;
            max = room.conf.max;
            interval = room.conf.interval;
            requiresPassword = room.RequiresPassword;
        }
    }
}
