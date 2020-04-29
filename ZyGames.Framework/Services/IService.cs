namespace ZyGames.Framework.Services
{
    public interface IService : IAddressable
    {
        bool IsAlived { get; }

        object Metadata { get; }

        T GetMetadata<T>();
    }

    //public interface IServiceWithGuidKey : IService
    //{ }

    //public interface IServiceWithIntegerKey : IService
    //{ }

    //public interface IServiceWithStringKey : IService
    //{ }
}