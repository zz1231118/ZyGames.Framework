namespace ZyGames.Framework.Services
{
    public interface IAddressable
    {
        SlioAddress Address { get; }

        Identity Identity { get; }
    }
}
