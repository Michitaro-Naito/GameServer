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
    }
}
