using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public static class UT
    {
        static int _seed = 1000 * DateTime.Now.Second + DateTime.Now.Millisecond;
        public static int Random(int minValue, int maxValue)
        {
            var rand = new Random(_seed);
            _seed = rand.Next();
            return rand.Next(minValue, maxValue);
        }

        public static T Random<T>(params T[] objs)
        {
            if (objs == null || objs.Length == 0) return default(T);
            return objs[Random(0, objs.Length)];
        }

        public static bool RandomRate(double rate)
        {
            if (rate < 0 || 1 < rate)
                throw new ArgumentException("rate must be >= 0 and <= 1");
            if (rate == 1)
                return true;
            return rate > Random(0, 100000000) / 100000000.0;
        }

        public static string RandomString(uint length)
        {
            var builder = new StringBuilder();
            char ch;
            for (int i = 0; i < length; i++)
            {
                ch = Convert.ToChar(Random(0, 26) + 65);
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public static T Max<T>(params T[] values)
            where T : IComparable
        {
            if (values == null) return default(T);
            var max = default(T);
            foreach (var v in values)
            {
                if (v.CompareTo(max) > 0) max = v;
            }
            return max;
        }
    }

    public static class UTExtension
    {

        public static T RandomElement<T>(this IEnumerable<T> objs)
        {
            if (objs == null) return default(T);
            if (objs.Count() == 0) return default(T);
            return objs.ToList()[UT.Random(0, objs.Count())];
        }

        public static int RandomRound(this double value)
        {
            var rate = value - Math.Floor(value);
            if (UT.RandomRate(rate)) return (int)Math.Ceiling(value);
            return (int)Math.Floor(value);
        }

        public static List<string> Keys(this System.Resources.ResourceManager resourceManager)
        {
            var keys = new List<string>();
            var set = resourceManager.GetResourceSet(new System.Globalization.CultureInfo("en-US"), true, true);
            foreach (System.Collections.DictionaryEntry entry in set)
            {
                keys.Add(entry.Key.ToString());
            }
            return keys;
        }

        public static string RandomKey(this System.Resources.ResourceManager resourceManager)
        {
            return resourceManager.Keys().RandomElement();
        }

        public static void Do(this List<Func<bool>> funcs)
        {
            if (funcs == null) return;
            foreach (var func in funcs)
            {
                if (func()) break;
            }
        }

        public static string ToKey(this Enum obj)
        {
            return obj.GetType().Name + "_" + obj.ToString();
        }
        public static string ToLocalizedString(this Enum obj, CultureInfo culture)
        {
            var key = obj.ToKey();
            var str = MyResources._Enum.ResourceManager.GetString(key, culture);
            if (str == null)
                return key;
            return str;
        }
        public static string GetLocalizedDescription(this Enum obj, CultureInfo culture)
        {
            var key = obj.ToKey();
            var str = MyResources._EnumDescription.ResourceManager.GetString(key, culture);
            if (str == null)
                return key;
            return str;
        }



        public static Color Elevate(this Color color, int value)
        {
            if (color == null) throw new ArgumentNullException();

            return Color.FromArgb(
                Math.Max(0, Math.Min(255, color.R + value)),
                Math.Max(0, Math.Min(255, color.G + value)),
                Math.Max(0, Math.Min(255, color.B + value))
            );
        }

        public static byte GetBrightness255(this Color color)
        {
            return UT.Max(color.R, color.G, color.B);
        }
    }
}
