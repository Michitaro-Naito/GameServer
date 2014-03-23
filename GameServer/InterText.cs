using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class InterText
    {
        public enum InterTextType
        {
            Text,
            Title,
            MaleName,
            FemaleName
        }

        InterTextType _type;
        string _key;

        public InterText(string key, InterTextType type = InterTextType.Text)
        {
            if (key == null)
                throw new ArgumentNullException("key must not be null.");
            _type = type;
            _key = key;
        }

        public override string ToString()
        {
            return string.Format("[{0} {1}]", _type, _key);
            //return string.Format("[InterText {0} {1}]", _type, _key);
        }

        public string GetString(CultureInfo culture)
        {
            var str = (string)null;
            switch (_type)
            {
                case InterTextType.Text:
                default:
                    str = MyResources._.ResourceManager.GetString(_key, culture);
                    break;

                case InterTextType.Title:
                    str = MyResources._Title.ResourceManager.GetString(_key, culture);
                    break;

                case InterTextType.MaleName:
                    str = MyResources._MaleName.ResourceManager.GetString(_key, culture);
                    break;

                case InterTextType.FemaleName:
                    str = MyResources._FemaleName.ResourceManager.GetString(_key, culture);
                    break;
            }
            if (str == null)
                str = ToString();
            return str;
        }

        public string GetStringFor(Player player)
        {
            if (player == null)
                throw new ArgumentNullException("player must not be null.");
            return GetString(player.Culture);
        }
    }
}
