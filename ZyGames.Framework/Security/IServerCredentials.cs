namespace ZyGames.Framework.Security
{
    public interface IServerCredentials
    {
        string AccessKey { get; }

        ServerCredentialsValidator Validator { get; }

        bool Authenticate(IAuthorization authorization);
    }
}
