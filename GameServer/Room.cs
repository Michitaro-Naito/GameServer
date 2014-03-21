using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class Room
    {
        public int roomId;
        public string guid;
        List<Character> _characters = new List<Character>();

        public bool IsConfigured { get; private set; }

        public bool IsEmpty
        {
            get
            {
                return _characters.Count == 0;
            }
        }

        public Room()
        {
            guid = Guid.NewGuid().ToString();
            IsConfigured = false;
        }

        public override string ToString()
        {
            return string.Format("[Room roomId:{0} guid:{1}]", roomId, guid);
        }

        public bool HasCharacter(Character character)
        {
            return _characters.Any(c => c == character);
        }

        public void Add(Character character)
        {
            _characters.Add(character);
        }

        public void RemoveAll(Player player)
        {
            _characters.RemoveAll(c => c.Player == player);
        }

        public void Command(MyHub hub, Character character, List<string> parameters)
        {
            hub.SystemMessage("Room.Command()");
            if (parameters.Count == 0)
            {
                hub.SystemMessage("No params...");
                return;
            }
            switch (parameters[0])
            {
                case "GetCharacters":
                    hub.SystemMessage(string.Format("Characters at {0}:", this));
                    _characters.ForEach(c => hub.SystemMessage(c.ToString()));
                    break;
                case "Chat":
                    _characters.ForEach(c => hub.SystemMessage(c.Player, parameters[1]));
                    break;
                case "Configure":
                    IsConfigured = true;
                    break;
                default:
                    hub.SystemMessage("Unknown RoomCommand: " + parameters[0]);
                    break;
            }
        }
    }
}
