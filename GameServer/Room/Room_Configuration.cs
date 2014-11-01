using MyResources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GameServer
{
    public partial class Room
    {
        public enum CharacterNameSet {
            Default,
            Japanese
        }

        /// <summary>
        /// Client model to validate.
        /// </summary>
        public class ClientConfiguration : IValidatable
        {
            public string name;
            public string password;
            public Nullable<int> max;
            public Nullable<int> interval;

            public bool noFirstDayFortuneTelling = false;
            public bool noPrivateMessage = false;
            public bool strongShaman = false;
            public bool hideCharacterNames = false;
            public CharacterNameSet characterNameSet;

            public string ModelName { get; set; }
            public ValidationResult Validate()
            {
                var result = new ValidationResult();

                if (name == null || name.Length == 0 || name.Length > 20)
                    result.Errors.Add(new InterText("AMustBeBToCCharacters", _Error.ResourceManager, new []{
                        new InterText("Room_Name", _Model.ResourceManager),
                        new InterText("1", null),
                        new InterText("20", null)
                    }));

                if (password != null && password.Length > 20)
                    result.Errors.Add(new InterText("AMustBeUpToBCharactersOrNull", _Error.ResourceManager, new[]{
                        new InterText("Room_Password", _Model.ResourceManager),
                        new InterText("20", null)
                    }));

                if (max == null || max < 7 || max > 32)
                    result.Errors.Add(new InterText("AMustBeBToC", _Error.ResourceManager, new[]{
                        new InterText("Room_Max", _Model.ResourceManager),
                        new InterText("7", null),
                        new InterText("32", null)
                    }));

                if (interval == null || interval < 30 || interval > 900)
                    result.Errors.Add(new InterText("AMustBeBToC", _Error.ResourceManager, new[]{
                        new InterText("Room_Interval", _Model.ResourceManager),
                        new InterText("30", null),
                        new InterText("900", null)
                    }));

                return result;
            }

            public Configuration ToConfiguration()
            {
                return new Configuration() {
                    name = name,
                    password = password,
                    max = max.Value,
                    interval = interval.Value,

                    noFirstDayFortuneTelling = noFirstDayFortuneTelling,
                    noPrivateMessage = noPrivateMessage,
                    strongShaman = strongShaman,
                    hideCharacterNames = hideCharacterNames,
                    characterNameSet = characterNameSet
                };
            }
        }

        /// <summary>
        /// Internal, real model.
        /// </summary>
        public class Configuration
        {
            public string name;
            public string password;
            public int max;
            public int interval;

            public bool noFirstDayFortuneTelling = false;
            public bool noPrivateMessage = false;
            public bool strongShaman = false;
            public bool hideCharacterNames = false;
            public CharacterNameSet characterNameSet;

            public CultureInfo culture;
            public TimeZoneInfo TimeZone
            {
                get
                {
                    if (culture.ToString() == "ja-JP")
                        return TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                    return TimeZoneInfo.Utc;
                }
            }

            public Configuration()
            {
                culture = new CultureInfo("ja-JP");
            }

            public string ToHtml()
            {
                var html = "";
                html += string.Format(
                    "<div>{0}: {1}</div>",
                    _Model.ResourceManager.GetString("Room_Name", culture),
                    HttpUtility.HtmlEncode(name));
                html += string.Format(
                    "<div>{0}: {1}</div>",
                    _Model.ResourceManager.GetString("Room_Max", culture),
                    HttpUtility.HtmlEncode(max.ToString()));
                html += string.Format("<div>{0}: {1}</div>",
                    _Model.ResourceManager.GetString("Room_Interval", culture),
                    HttpUtility.HtmlEncode(interval.ToString()));
                // TODO: add here
                html += string.Format("<div>{0}: {1}</div>",
                    _Model.ResourceManager.GetString("Room_NoFirstDayFortuneTelling", culture),
                    HttpUtility.HtmlEncode(noFirstDayFortuneTelling.ToString()));
                html += string.Format("<div>{0}: {1}</div>",
                    _Model.ResourceManager.GetString("Room_NoPrivateMessage", culture),
                    HttpUtility.HtmlEncode(noPrivateMessage.ToString()));
                html += string.Format("<div>{0}: {1}</div>",
                    _Model.ResourceManager.GetString("Room_StrongShaman", culture),
                    HttpUtility.HtmlEncode(strongShaman.ToString()));
                html += string.Format("<div>{0}: {1}</div>",
                    _Model.ResourceManager.GetString("Room_CharacterNameSet", culture),
                    HttpUtility.HtmlEncode(characterNameSet.ToString()));
                return html;
            }
        }
    }
}
