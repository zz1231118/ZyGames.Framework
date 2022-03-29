using Framework.Injection;

namespace ZyGames.Framework.Services
{
    public abstract class Service : Addressable, IService
    {
        public IContainer Container { get; internal set; }

        public IServiceFactory ServiceFactory { get; internal set; }

        public sealed override Address Address { get; internal set; }

        public sealed override Identity Identity { get; internal set; }

        public bool IsAlived => true;

        public object Metadata { get; internal set; }

        public T GetMeta<T>()
            where T : class
        {
            return Metadata as T;
        }

        public T GetMetadata<T>()
            where T : class
        {
            return Metadata as T;
        }
    }
}