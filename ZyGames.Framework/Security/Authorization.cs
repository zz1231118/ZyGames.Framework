using System;

namespace ZyGames.Framework.Security
{
    [Serializable]
    public sealed class Authorization : IAuthorization
    {
        public Authorization(string account, long timestamp, string token)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            Account = account;
            Timestamp = timestamp;
            Token = token;
        }

        public string Account { get; }

        public long Timestamp { get; }

        public string Token { get; }
    }
}
