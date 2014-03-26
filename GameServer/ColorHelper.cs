using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public static class ColorHelper
    {
        public class ColorIdentity
        {
            public string text;
            public string background;

            public override string ToString()
            {
                return string.Format("{0} {1}", text, background);
            }
        }

        public static ColorIdentity GenerateColorIdentity(string uniqueString)
        {
            byte[] bytes;
            using (var sha = new SHA256Managed())
            {
                bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(uniqueString));
            }

            var color = Color.FromArgb(bytes[0], bytes[1], bytes[2]);
            for (int n = 0; n < 10; n++)
            {
                var avg = (color.R + color.G + color.B) / 3;
                if (avg < 128)
                {
                    // Darken
                    color = color.Elevate(-64);
                    if (color.GetBrightness255() < 64) break;
                }
                else
                {
                    // Brighten
                    color = color.Elevate(64);
                    if (color.GetBrightness255() >= 192) break;
                }
            }

            var bgColor = Color.FromArgb(bytes[3], bytes[4], bytes[5]);
            for (int n = 0; n < 10; n++)
            {
                var offset = 0;
                var avg = ((int)color.R + color.G + color.B) / 3;
                if (avg < 128) offset += 64;
                else offset -= 64;
                bgColor = bgColor.Elevate(offset);

                if (Math.Abs((int)color.GetBrightness255() - (int)bgColor.GetBrightness255()) > 128)
                {
                    break;
                }
            }

            return new ColorIdentity
            {
                text = string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B),
                background = string.Format("#{0:X2}{1:X2}{2:X2}", bgColor.R, bgColor.G, bgColor.B)
            };
        }
    }
}
