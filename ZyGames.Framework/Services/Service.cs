using System;

namespace ZyGames.Framework.Services
{
    public abstract class Service : Addressable, IService
    {
        public IServiceFactory ServiceFactory { get; internal set; }

        public IServiceProvider ServiceProvider { get; internal set; }

        public sealed override SlioAddress Address { get; internal set; }

        public sealed override Identity Identity { get; internal set; }

        public bool IsAlived => true;

        public object Metadata { get; internal set; }

        public T GetMetadata<T>()
        {
            return (T)Metadata;
        }
    }
}