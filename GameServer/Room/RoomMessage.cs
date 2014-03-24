using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class RoomMessageInfo
    {
        public int id;
        public string callerUserId;
        public RoomMessage.Mode mode;
        public Nullable<int> fromId;
        public Nullable<int> toId;
        public string body;

        public RoomMessageInfo(RoomMessage message)
        {
            id = message.id;
            callerUserId = message.callerUserId;
            mode = message.mode;
            if (message.from != null)
                fromId = message.from.id;
            if (message.to != null)
                toId = message.to.id;
            body = message.body;
        }
    }

    class RoomMessage
    {
        public enum Mode
        {
            All = 0,
            Wolf = 1,
            Ghost = 2,
            Private = 3
        }

        public int id;
        public string callerUserId;
        public Mode mode;
        public Actor from;
        public Actor to;
        public string body;
    }
}
