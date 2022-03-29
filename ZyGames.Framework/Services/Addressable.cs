namespace ZyGames.Framework.Services
{
    public abstract class Addressable : IAddressable
    {
        public abstract Address Address { get; internal set; }

        public abstract Identity Identity { get; internal set; }

        protected internal virtual void Initialize()
        { }

        protected internal virtual void Start()
        { }

        protected internal virtual void Stop()
        { }
    }
}
