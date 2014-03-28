using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class RoomMessageInfo
    {
        public int id;
        public DateTime Created;
        public string callerUserId;
        public RoomMessage.Mode mode;
        public Nullable<int> fromId;
        public Nullable<int> toId;
        public string[] bodyRows;

        public RoomMessageInfo(RoomMessage message, CultureInfo culture)
        {
            id = message.id;
            Created = message.Created;
            callerUserId = message.callerUserId;
            mode = message.mode;
            if (message.from != null)
                fromId = message.from.id;
            if (message.to != null)
                toId = message.to.id;
            //body = message.body;
            bodyRows = message.bodyRows.Select(r => r.GetString(culture)).ToArray();
        }
    }

    class RoomMessage
    {
        public enum Mode : int
        {
            All = 0,
            Wolf = 1,
            Ghost = 2,
            Private = 3
        }

        public class ModeInfo
        {
            public Mode id;
            public string name;

            public ModeInfo(Player player, Mode mode)
            {
                id = mode;
                name = mode.ToLocalizedString(player.Culture);
            }
        }

        public int id;
        public DateTime Created;
        public string callerUserId;
        public Mode mode;
        public Actor from;
        public Actor to;
        public InterText[] bodyRows;

        public bool IsVisibleFor(Room room, Actor viewer)
        {
            if (new RoomState[] { RoomState.Matchmaking, RoomState.Playing }.Contains(room.RoomState))
            {
                switch (mode)
                {
                    case Mode.All:
                        // Always visible for anybody.
                        return true;

                    case Mode.Wolf:
                        // Only for werewolves.
                        if (viewer != null && viewer.role == Role.Werewolf)
                            return true;
                        return false;

                    case Mode.Ghost:
                        // Only for dead guys.
                        if (viewer != null && viewer.IsDead)
                            return true;
                        return false;

                    case Mode.Private:
                        // Only for sent or received guys.
                        return viewer == from || viewer == to;

                    default:
                        // Unknown
                        return false;
                }
            }
            else
            {
                // Ending or Ended. Always visible.
                return true;
            }
        }
    }
}
