namespace ZyGames.Framework.Services.Networking
{
    internal interface IConnectionManager
    {
        void Connected(Address address, Connection connection);

        void ConnectionTerminated(Address address, Connection connection);

        Connection Allocate(Address endpoint);

        void Return(Address endpoint, Connection connection);
    }
}
