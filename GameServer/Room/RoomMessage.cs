using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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
                        if (viewer != null && /*viewer.role == Role.Werewolf*/ viewer.CanShareWerewolfCommunity)
                            return true;
                        return false;

                    case Mode.Ghost:
                        // Only for dead guys.
                        if (viewer != null && viewer.IsDead)
                            return true;
                        return false;

                    case Mode.Private:
                        if (viewer != null && viewer.CanShareLoverCommunity && !from.CanShareLoverCommunity)
                            // Sent from Non-lover to Lover. Lover never read mesages except from the partner. ;)
                            return false;
                        // Only for sent or received guys.
                        return viewer != null && (viewer == from || viewer == to);

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

        public string ToHtml(CultureInfo culture)
        {
            var headerHtml = "";
            var timeZone = TimeZoneInfo.Utc;
            if(culture.ToString()=="ja-JP")
                timeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            var localCreated = TimeZoneInfo.ConvertTimeFromUtc(Created, timeZone);

            if (from != null)
            {
                var name = "";
                name = HttpUtility.HtmlEncode(from.TitleAndName.GetString(culture));
                headerHtml += string.Format("<span class=\"name\" style=\"color:{0};background-color:{1};\">{2}</span>",
                    from.ColorIdentity.text, from.ColorIdentity.background, name);
            }
            
            headerHtml += string.Format("<span class=\"time\">{0}</span>", localCreated.ToString("HH:mm"));

            if (from != null)
            {
                var toString = "";
                if (mode == Mode.Private && to != null)
                    toString = to.TitleAndName.GetString(culture);
                else
                    toString = mode.ToLocalizedString(culture);
                headerHtml += string.Format("<span class=\"to\">To {0}</span>", HttpUtility.HtmlEncode(toString));
            }

            var internalHtml = "";
            foreach (var row in bodyRows)
            {
                internalHtml += string.Format("<div>{0}</div>", HttpUtility.HtmlEncode(row.GetString(culture)));
            }
            return string.Format("<li class=\"mode{0}\"><div class=\"from\">{1}</div><div class=\"body\">{2}</div></li>",
                (int)mode, headerHtml, internalHtml);
        }
    }
}
