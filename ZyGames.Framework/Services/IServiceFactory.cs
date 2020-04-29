using System;
using System.Collections.Generic;
using ZyGames.Framework.Services.Lifecycle;

namespace ZyGames.Framework.Services
{
    public interface IServiceFactory
    {
        IMembershipLifecycle MembershipLifecycle { get; }

        IService NewService(Type serviceType, Type serviceInterfaceType, Identity identity = null, object metadata = null);

        TServiceInterface NewService<TServiceInterface, TService>(Identity identity = null, object metadata = null)
            where TServiceInterface : IService
            where TService : Service, TServiceInterface;

        IService GetService(Identity identity, bool throwOnError = true);

        IReadOnlyList<IService> GetServices(Type serviceInterfaceType);

        TServiceInterface GetService<TServiceInterface>(Identity identity, bool throwOnError = true)
            where TServiceInterface : IService;

        IReadOnlyList<TServiceInterface> GetServices<TServiceInterface>()
            where TServiceInterface : IService;

        TServiceInterface FindService<TServiceInterface>(Func<TServiceInterface, bool> predicate = null)
            where TServiceInterface : IService;

        IReadOnlyList<TServiceInterface> FindAllService<TServiceInterface>(Func<TServiceInterface, bool> predicate = null)
            where TServiceInterface : IService;

        TServiceInterface GetSingleService<TServiceInterface>()
            where TServiceInterface : IService;

        void KillService(Identity identity);

        TServiceObserverInterface CreateObjectReference<TServiceObserverInterface>(IServiceObserver obj)
            where TServiceObserverInterface : IServiceObserver;

        void DeleteObjectReference(IServiceObserver obj);
    }
}
