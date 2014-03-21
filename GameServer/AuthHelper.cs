using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace AuthUtility
{
    /// <summary>
    /// Encapsulates JSON data for ID token body.
    /// </summary>
    public class IDTokenJsonBodyObject
    {
        public string iss;
        public string aud;
        public string at_hash;
        public string azp;
        public string c_hash;
        public string sub;
        public int iat;
        public int exp;
    }

    public class AuthHelper
    {
        /// <summary>
        /// Gets Google+ ID from authObject.id_token of DotNetOAuth.
        /// </summary>
        /// <param name="id_token"></param>
        /// <returns></returns>
        public static string GetGooglePlusId(string id_token)
        {
            string[] segments = id_token.Split('.');
            string base64EncoodedJsonBody = segments[1];
            int mod4 = base64EncoodedJsonBody.Length % 4;
            if (mod4 > 0)
            {
                base64EncoodedJsonBody += new string('=', 4 - mod4);
            }
            byte[] encodedBodyAsBytes =
                Convert.FromBase64String(base64EncoodedJsonBody);
            var json_body = Encoding.UTF8.GetString(encodedBodyAsBytes);
            var bodyObject = JsonConvert.DeserializeObject<IDTokenJsonBodyObject>(json_body);
            return bodyObject.sub;
        }

        /// <summary>
        /// Computes Base64 encoded, SHA512 Hash.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Hash(string secretString, byte[] bytes)
        {
            var key = Encoding.UTF8.GetBytes(secretString);
            byte[] hash;
            using (var sha = new System.Security.Cryptography.SHA512Managed())
            {
                hash = sha.ComputeHash(bytes.Concat(key).ToArray());
            }
            return Convert.ToBase64String(hash);
        }
        public static string Hash(string secretString, string str)
        {
            return Hash(secretString, Encoding.UTF8.GetBytes(str));
        }

        // MD5 hash, less secure.
        public static string MD5Hash(string secretString, byte[] bytes)
        {
            var key = Encoding.UTF8.GetBytes(secretString);
            byte[] hash;
            using (var sha = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                hash = sha.ComputeHash(bytes.Concat(key).ToArray());
            }
            return Convert.ToBase64String(hash);
        }
        public static string MD5Hash(string secretString, string str)
        {
            return MD5Hash(secretString, Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Computes AES512 encrypted, Base64 encoded string.
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Encrypt(string plainText, byte[] key, byte[] iv)
        {
            using (var aes = new AesManaged() { Key = key, IV = iv })
            using (var enc = aes.CreateEncryptor(key, iv))
            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
            {
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        public static string Encrypt(string plainText, string key, string iv)
        {
            return Encrypt(plainText, Convert.FromBase64String(key), Convert.FromBase64String(iv));
        }

        public static string Decrypt(string encodedCipherText, byte[] key, byte[] iv)
        {
            using (var aes = new AesManaged() { Key = key, IV = iv })
            using (var dec = aes.CreateDecryptor(key, iv))
            using (var ms = new MemoryStream(Convert.FromBase64String(encodedCipherText)))
            using (var cs = new CryptoStream(ms, dec, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
        public static string Decrypt(string encodedCipherText, string key, string iv)
        {
            return Decrypt(encodedCipherText, Convert.FromBase64String(key), Convert.FromBase64String(iv));
        }
    }
}