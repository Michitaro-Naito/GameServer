using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthUtility
{
    public class GamePass
    {
        public class GamePassData
        {
            // Auth (Server trusts it.)
            public string userId;

            // Cache (Server doesn't trust it.)
        }

        public GamePassData data = new GamePassData();

        public override string ToString()
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public string ToCipher(string key, string iv)
        {
            var json = JsonConvert.SerializeObject(this);
            return AuthHelper.Encrypt(json, key, iv);
        }

        public static GamePass FromCipher(string cipher, string key, string iv)
        {
            var json = AuthHelper.Decrypt(cipher, key, iv);
            return JsonConvert.DeserializeObject<GamePass>(json);
        }
    }
}
