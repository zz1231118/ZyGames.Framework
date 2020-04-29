using System;
using System.Security.Cryptography;
using System.Text;

namespace ZyGames.Framework.Security
{
    public class ClientCredentials : IClientCredentials
    {
        private readonly string account;
        private readonly string accessKey;

        public ClientCredentials(string account, string accessKey)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));
            if (accessKey == null)
                throw new ArgumentNullException(nameof(accessKey));

            this.account = account;
            this.accessKey = accessKey;
        }

        public string Account => account;

        public string AccessKey => accessKey;

        private static long ToTimestamp(DateTime time)
        {
            return (time.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }

        internal static string Md5(string encryptText)
        {
            var bytes = Encoding.UTF8.GetBytes(encryptText);
            using (var md5 = MD5.Create())
            {
                bytes = md5.ComputeHash(bytes);
            }
            var sb = new StringBuilder(32);
            foreach (var by in bytes)
            {
                sb.Append(by.ToString("x2"));
            }
            return sb.ToString();
        }

        public IAuthorization Create()
        {
            long timestamp = ToTimestamp(DateTime.Now.ToUniversalTime());
            var tokenText = account + accessKey + timestamp;
            var array = tokenText.ToCharArray();

            Array.Sort(array);
            var token = Md5(new string(array));
            return new Authorization(account, timestamp, token);
        }
    }
}