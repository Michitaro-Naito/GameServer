using MyResources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class Error
    {
        public class ErrorInfo
        {
            public string title;
            public string body;
        }

        public InterText Title { get; set; }
        public InterText Body { get; set; }

        public ErrorInfo GetInfo(CultureInfo culture)
        {
            return new ErrorInfo() { title = Title.GetString(culture), body = Body.GetString(culture) };
        }

        public static Error Create(string titleKey, string bodyKey)
        {
            return new Error()
            {
                Title = new InterText(titleKey, _Error.ResourceManager),
                Body = new InterText(bodyKey, _Error.ResourceManager)
            };
        }
    }
}
