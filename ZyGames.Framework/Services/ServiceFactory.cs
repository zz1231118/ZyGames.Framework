using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Directory;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Membership;
using ZyGames.Framework.Services.Options;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services
{
    internal class ServiceFactory : ISystemServiceFactory, ILifecycleObserver
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ActivationDirectory activationDirectory;
        private readonly AddressableDirectory addressableDirectory;
        private readonly AddressableTypeManager addressableTypeManager;
        private readonly ReferenceRuntime referenceRuntime;
        private readonly GatewayMembershipServiceOptions membershipServiceOptions;
        private readonly MembershipManager membershipManager;
        private readonly IServiceHostLifecycle hostingLifecycle;
        private readonly IMembershipLifecycle membershipLifecycle;
        private readonly Func<Identity, IService> serviceReferenceFactory;
        private IClusterMembershipService clusterMembershipService;

        public IMembershipLifecycle MembershipLifecycle => membershipLifecycle;

        public ServiceFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.activationDirectory = serviceProvider.GetRequiredService<ActivationDirectory>();
            this.addressableDirectory = serviceProvider.GetRequiredService<AddressableDirectory>();
            this.addressableTypeManager = serviceProvider.GetRequiredService<AddressableTypeManager>();
            this.referenceRuntime = serviceProvider.GetRequiredService<ReferenceRuntime>();
            this.membershipServiceOptions = serviceProvider.GetService<GatewayMembershipServiceOptions>();
            this.membershipManager = serviceProvider.GetRequiredService<MembershipManager>();
            this.hostingLifecycle = serviceProvider.GetRequiredService<IServiceHostLifecycle>();
            this.membershipLifecycle = serviceProvider.GetRequiredService<IMembershipLifecycle>();
            this.serviceReferenceFactory = new Func<Identity, IService>(CastServiceReference);
            this.hostingLifecycle.Subscribe(nameof(ServiceFactory), Lifecycles.Stage.System, this);
        }

        private IService GetService(ServiceLocator locator)
        {
            return (IService)addressableDirectory.GetAddressable(locator.Identity, key =>
            {
                var serviceTypeData = addressableTypeManager.GetServiceTypeData(locator.InterfaceType);
                return serviceTypeData.CreateServiceReference(serviceProvider, referenceRuntime, locator.Address, locator.Identity, locator.Metadata);
            });
        }

        private IService CastServiceReference(Identity identity)
        {
            var serviceLocator = membershipManager.GetServiceLocator(identity);
            if (serviceLocator == null)
            {
                //service not found
                throw new ServiceNotFoundException(identity);
            }

            var serviceTypeData = addressableTypeManager.GetServiceTypeData(serviceLocator.InterfaceType);
            return serviceTypeData.CreateServiceReference(serviceProvider, referenceRuntime, serviceLocator.Address, identity, serviceLocator.Metadata);
        }

        internal SlioAddress GetSlioAddress(Identity identity)
        {
            if (addressableDirectory.TryGetAddressable(identity, out IAddressable addressable))
            {
                return addressable.Address;
            }

            var locator = membershipManager.GetServiceLocator(identity);
            if (locator == null) return null;
            return locator.Address;
        }

        internal void NewSystemTarget(Type systemTargetType, Type systemTargetInterfaceType)
        {
            var systemTarget = (SystemTarget)Activator.CreateInstance(systemTargetType, true);
            systemTarget.ServiceFactory = this;
            systemTarget.ServiceProvider = serviceProvider;
            systemTarget.Initialize();

            var invokeContextCategory = InvokeContextCategory.Multi;
            var systemTargetTypeData = addressableTypeManager.GetSystemTargetTypeData(systemTargetInterfaceType);
            var methodInvoker = systemTargetTypeData.CreateMethodInvoker();
            var activation = new ActivationData(systemTarget, methodInvoker, systemTargetInterfaceType, systemTarget.Priority, invokeContextCategory);
            activationDirectory.RegisterTarget(activation);
        }

        public IService NewService(Type serviceType, Type serviceInterfaceType, Identity identity = null, object metadata = null)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (serviceInterfaceType == null)
                throw new ArgumentNullException(nameof(serviceInterfaceType));
            if (membershipServiceOptions == null)
                throw new InvalidOperationException("Gateway service not found.");

            var service = (Service)Activator.CreateInstance(serviceType);
            service.ServiceFactory = this;
            service.ServiceProvider = serviceProvider;
            service.Identity = identity ?? Identity.NewIdentity(Identity.Categories.Service);
            service.Address = membershipServiceOptions.OutsideAddress;
            service.Metadata = metadata;
            service.Initialize();

            var invokeContextCategory = InvokeContextCategory.Multi;
            var serviceContract = serviceInterfaceType.GetCustomAttribute<ServiceContractAttribute>(false);
            if (serviceContract != null)
            {
                invokeContextCategory = serviceContract.InvokeContextCategory;
            }

            var serviceTypeData = addressableTypeManager.GetServiceTypeData(serviceInterfaceType);
            var methodInvoker = serviceTypeData.CreateMethodInvoker();
            var activation = new ActivationData(service, methodInvoker, serviceInterfaceType, Priority.User, invokeContextCategory);
            activationDirectory.RegisterTarget(activation);

            if (hostingLifecycle.Status >= ServiceHostStatus.Joining)
            {
                InvokerContext.Caller = service;

                try
                {
                    service.Start();
                }
                finally
                {
                    InvokerContext.Caller = null;
                }
            }

            var reference = serviceTypeData.CreateServiceReference(serviceProvider, referenceRuntime, service.Address, service.Identity, metadata);
            addressableDirectory.Add(reference);
            return reference;
        }

        public TSystemTargetInterface GetSystemTarget<TSystemTargetInterface>(Identity identity, SlioAddress destination)
            where TSystemTargetInterface : ISystemTarget
        {
            if (!addressableDirectory.TryGetAddressable(identity, out IAddressable addressable))
            {
                var systemTargetInterfaceType = typeof(TSystemTargetInterface);
                var systemTargetTypeData = addressableTypeManager.GetSystemTargetTypeData(systemTargetInterfaceType);
                addressable = systemTargetTypeData.CreateSystemTargetReference(serviceProvider, referenceRuntime, destination, identity);
                addressableDirectory.Add(addressable);
            }

            return (TSystemTargetInterface)addressable;
        }

        public TServiceInterface NewService<TServiceInterface, TService>(Identity identity = null, object metadata = null)
            where TServiceInterface : IService
            where TService : Service, TServiceInterface
        {
            var serviceType = typeof(TService);
            var serviceInterfaceType = typeof(TServiceInterface);
            return (TServiceInterface)NewService(serviceType, serviceInterfaceType, identity, metadata);
        }

        public IService GetService(Identity identity, bool throwOnError)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            if (throwOnError)
            {
                return (IService)addressableDirectory.GetAddressable(identity, serviceReferenceFactory);
            }
            if (!addressableDirectory.TryGetAddressable(identity, out IAddressable addressable))
            {
                var locator = membershipManager.GetServiceLocator(identity);
                if (locator == null) return null;

                var serviceTypeData = addressableTypeManager.GetServiceTypeData(locator.InterfaceType);
                addressable = serviceTypeData.CreateServiceReference(serviceProvider, referenceRuntime, locator.Address, locator.Identity, locator.Metadata);
                addressableDirectory.Add(addressable);
            }
            return (IService)addressable;
        }

        public IReadOnlyList<IService> GetServices(Type serviceInterfaceType)
        {
            if (serviceInterfaceType == null)
                throw new ArgumentNullException(nameof(serviceInterfaceType));

            var services = new List<IService>();
            var locators = membershipManager.GetServiceLocators(serviceInterfaceType);
            foreach (var locator in locators)
            {
                services.Add(GetService(locator));
            }
            return services;
        }

        public TServiceInterface GetService<TServiceInterface>(Identity identity, bool throwOnError)
            where TServiceInterface : IService
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            return (TServiceInterface)GetService(identity, throwOnError);
        }

        public IReadOnlyList<TServiceInterface> GetServices<TServiceInterface>()
            where TServiceInterface : IService
        {
            var services = new List<TServiceInterface>();
            var serviceInterfaceType = typeof(TServiceInterface);
            var locators = membershipManager.GetServiceLocators(serviceInterfaceType);
            foreach (var locator in locators)
            {
                services.Add((TServiceInterface)GetService(locator));
            }
            return services;
        }

        public TServiceInterface FindService<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService
        {
            var services = GetServices<TServiceInterface>();
            return predicate != null ? services.FirstOrDefault(predicate) : services.FirstOrDefault();
        }

        public IReadOnlyList<TServiceInterface> FindAllService<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService
        {
            var services = GetServices<TServiceInterface>();
            return predicate != null ? services.Where(predicate).ToList() : services;
        }

        public TServiceInterface GetSingleService<TServiceInterface>()
            where TServiceInterface : IService
        {
            return GetServices<TServiceInterface>().Single();
        }

        public void KillService(Identity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (clusterMembershipService == null)
                throw new InvalidOperationException("Gateway service not found.");

            clusterMembershipService.KillService(identity);
        }

        public TServiceObserverInterface CreateObjectReference<TServiceObserverInterface>(IServiceObserver obj)
            where TServiceObserverInterface : IServiceObserver
        {
            throw new NotImplementedException();
        }

        public void DeleteObjectReference(IServiceObserver obj)
        {
            throw new NotImplementedException();
        }

        void ILifecycleObserver.Notify(CancellationToken token, int state)
        {
            switch (state)
            {
                case Lifecycles.State.ServiceHost.Started:
                    if (membershipServiceOptions != null)
                    {
                        clusterMembershipService = GetSystemTarget<IClusterMembershipService>(Constants.ClusterMembershipServiceIdentity, membershipServiceOptions.Cluster);
                    }
                    break;
            }
        }
    }
}
