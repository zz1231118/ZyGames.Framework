using System;

namespace ZyGames.Framework.Security
{
    public class ServerCredentials : IServerCredentials
    {
        private readonly string accessKey;
        private readonly ServerCredentialsValidator validator;

        public ServerCredentials(string accessKey)
        {
            if (accessKey == null)
                throw new ArgumentNullException(nameof(accessKey));

            this.accessKey = accessKey;
        }

        public ServerCredentials(string accessKey, ServerCredentialsValidator validator)
            : this(accessKey)
        {
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            this.validator = validator;
        }

        public string AccessKey => accessKey;

        public ServerCredentialsValidator Validator => validator;

        public virtual bool Authenticate(IAuthorization authorization)
        {
            if (authorization == null)
                throw new ArgumentNullException(nameof(authorization));

            if (validator != null && !validator(authorization))
            {
                //验证失败
                return false;
            }

            var tokenText = authorization.Account + accessKey + authorization.Timestamp;
            var array = tokenText.ToCharArray();

            Array.Sort(array);
            var encryptText = new string(array);
            var token = ClientCredentials.Md5(encryptText);
            return token.Equals(authorization.Token, StringComparison.InvariantCulture);
        }
    }
}
