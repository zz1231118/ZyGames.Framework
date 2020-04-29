namespace ZyGames.Framework.Services
{
    public abstract class Addressable : IAddressable
    {
        public abstract SlioAddress Address { get; internal set; }

        public abstract Identity Identity { get; internal set; }

        internal protected virtual void Initialize()
        { }

        internal protected virtual void Start()
        { }

        internal protected virtual void Stop()
        { }
    }
}
