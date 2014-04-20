using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// Represents an international text.
    /// Can be localized to any languages.
    /// Can be cascaded like [[Alive] is damaged by [Bob]].
    /// </summary>
    public class InterText
    {
        /// <summary>
        /// ResourceManager of ResourceFile(.resx) to look up.
        /// eg. MyResources._.ResourceManager
        /// </summary>
        public ResourceManager ResourceManager { get; protected set; }

        /// <summary>
        /// StringKey to look up like "Foo".
        /// </summary>
        public string Key { get; protected set; }

        /// <summary>
        /// Parameters to format final text.
        /// </summary>
        public InterText[] Params { get; protected set; }

        /// <summary>
        /// Allocates InterText.
        /// </summary>
        /// <param name="key">String key to look up.</param>
        /// <param name="resourceManager">ResourceManager to look up. Can be null. (If null, output will be key.)</param>
        public InterText(string key, ResourceManager resourceManager, InterText[] parameters = null)
        {
            if (key == null)
                throw new ArgumentNullException("key must not be null.");
            Key = key;
            ResourceManager = resourceManager;
            Params = parameters;
        }

        public static InterText Create(object obj) {
            if (obj.GetType() == typeof(InterText))
                return (InterText)obj;
            return new InterText(obj.ToString(), null);
        }

        public static InterText Create(string key, ResourceManager resourceManager, params object[] parameters) {
            if (key == null)
                throw new ArgumentNullException("key must not be null.");
            InterText[] ps = null;
            if (parameters != null)
                ps = parameters.Select(p => InterText.Create(p)).ToArray();
            return new InterText(key, resourceManager, ps);
        }

        /// <summary>
        /// Gets localized string using culture.
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public string GetString(CultureInfo culture)
        {
            string str = null;
            if (ResourceManager != null)
                str = ResourceManager.GetString(Key, culture);
            if (str == null)
                str = Key;

            if (Params != null)
            {
                var localizedParams = Params.Select(p => p.GetString(culture)).ToArray();
                try
                {
                    str = string.Format(str, localizedParams);
                }
                catch
                {
                    // Failed to format. Returns current str...
                }
            }

            return str;
        }

        /// <summary>
        /// Gets localized string using en-US.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetString(new CultureInfo("en-US"));
            //return string.Format("[{0}]", Key);
        }

        public string GetStringFor(Player player)
        {
            if (player == null)
                throw new ArgumentNullException("player must not be null.");
            return GetString(player.Culture);
        }
    }
}
