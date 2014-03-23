using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public partial class Room
    {
        public void Queue(RoomCommand.Base command)
        {
            if (command == null)
                throw new ArgumentNullException("command must not be null.");
            _queue.Enqueue(command);
        }
    }
}
