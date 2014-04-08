using MyResources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public partial class Room
    {
        public class ClientConfiguration : IValidatable
        {
            public string name;
            public string password;
            public Nullable<int> max;
            public Nullable<int> interval;

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

                if (interval == null || interval < 5 || interval > 900)
                    result.Errors.Add(new InterText("AMustBeBToC", _Error.ResourceManager, new[]{
                        new InterText("Room_Interval", _Model.ResourceManager),
                        new InterText("5", null),
                        new InterText("900", null)
                    }));

                return result;
            }

            public Configuration ToConfiguration()
            {
                return new Configuration() { name = name, password = password, max = max.Value, interval = interval.Value };
            }
        }

        public class Configuration
        {
            public string name;
            public string password;
            public int max;
            public int interval;
            public CultureInfo culture;

            public Configuration()
            {
                culture = new CultureInfo("ja-JP");
            }
        }
    }
}
