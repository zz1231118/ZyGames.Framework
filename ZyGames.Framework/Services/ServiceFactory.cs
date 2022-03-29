using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Framework.Injection;
using ZyGames.Framework.Services.Directory;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Membership;
using ZyGames.Framework.Services.Options;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services
{
    internal class ServiceFactory : ISystemServiceFactory, ILifecycleObserver
    {
        private readonly IContainer container;
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

        public ServiceFactory(IContainer container)
        {
            this.container = container;
            this.activationDirectory = container.Required<ActivationDirectory>();
            this.addressableDirectory = container.Required<AddressableDirectory>();
            this.addressableTypeManager = container.Required<AddressableTypeManager>();
            this.referenceRuntime = container.Required<ReferenceRuntime>();
            this.membershipServiceOptions = container.Optional<GatewayMembershipServiceOptions>();
            this.membershipManager = container.Required<MembershipManager>();
            this.hostingLifecycle = container.Required<IServiceHostLifecycle>();
            this.membershipLifecycle = container.Required<IMembershipLifecycle>();
            this.serviceReferenceFactory = new Func<Identity, IService>(CastServiceReference);
            this.hostingLifecycle.Subscribe<ServiceFactory>(Lifecycles.Stage.System, this);
        }

        public IContainer Container => container;

        public IMembershipLifecycle MembershipLifecycle => membershipLifecycle;

        private IService GetService(ServiceLocator locator)
        {
            return (IService)addressableDirectory.GetAddressable(locator.Identity, key =>
            {
                var serviceTypeData = addressableTypeManager.GetServiceTypeData(locator.InterfaceType);
                return serviceTypeData.CreateServiceReference(container, referenceRuntime, locator.Address, locator.Identity, locator.Metadata);
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
            return serviceTypeData.CreateServiceReference(container, referenceRuntime, serviceLocator.Address, identity, serviceLocator.Metadata);
        }

        internal void NewSystemTarget(Type systemTargetType, Type systemTargetInterfaceType)
        {
            var priority = Priority.Application;
            var contract = systemTargetInterfaceType.GetCustomAttribute<SystemTargetContractAttribute>();
            if (contract != null) priority = contract.Priority;

            var systemTarget = (SystemTarget)Activator.CreateInstance(systemTargetType, true);
            systemTarget.ServiceFactory = this;
            systemTarget.Container = container;
            systemTarget.Initialize();

            var systemTargetTypeData = addressableTypeManager.GetSystemTargetTypeData(systemTargetInterfaceType);
            var methodInvoker = systemTargetTypeData.CreateMethodInvoker();
            var activation = new Activation(systemTarget, methodInvoker, systemTargetInterfaceType, priority);
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

            var contract = serviceInterfaceType.GetCustomAttribute<ServiceContractAttribute>(false);
            if (contract != null && identity == null && contract.Guid != null)
            {
                identity = new Identity(contract.Guid.Value, Identity.Categories.Service);
            }

            var service = (Service)Activator.CreateInstance(serviceType);
            service.ServiceFactory = this;
            service.Container = container;
            service.Identity = identity ?? Identity.NewIdentity(Identity.Categories.Service);
            service.Address = membershipServiceOptions.OutsideAddress;
            service.Metadata = metadata;
            service.Initialize();

            var serviceTypeData = addressableTypeManager.GetServiceTypeData(serviceInterfaceType);
            var methodInvoker = serviceTypeData.CreateMethodInvoker();
            var activation = new Activation(service, methodInvoker, serviceInterfaceType, Priority.Application);
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

            var reference = serviceTypeData.CreateServiceReference(container, referenceRuntime, service.Address, service.Identity, metadata);
            addressableDirectory.Add(reference);
            return reference;
        }

        public TSystemTargetInterface GetSystemTarget<TSystemTargetInterface>(Identity identity, Address destination)
            where TSystemTargetInterface : ISystemTarget
        {
            return (TSystemTargetInterface)addressableDirectory.GetAddressable(identity, key =>
            {
                var systemTargetInterfaceType = typeof(TSystemTargetInterface);
                var systemTargetTypeData = addressableTypeManager.GetSystemTargetTypeData(systemTargetInterfaceType);
                return systemTargetTypeData.CreateSystemTargetReference(referenceRuntime, destination, identity);
            });
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
                if (locator == null)
                {
                    return null;
                }
                addressable = addressableDirectory.GetAddressable(identity, key =>
                {
                    var serviceTypeData = addressableTypeManager.GetServiceTypeData(locator.InterfaceType);
                    return serviceTypeData.CreateServiceReference(container, referenceRuntime, locator.Address, locator.Identity, locator.Metadata);
                });
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

        public TServiceInterface GetService<TServiceInterface>(bool throwOnError)
            where TServiceInterface : IService
        {
            return throwOnError ? GetServices<TServiceInterface>().First() : GetServices<TServiceInterface>().FirstOrDefault();
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

        public TServiceInterface GetService<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var services = GetServices<TServiceInterface>();
            return services.FirstOrDefault(predicate);
        }

        public IReadOnlyList<TServiceInterface> GetServices<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var services = GetServices<TServiceInterface>();
            return services.Where(predicate).ToList();
        }

        public TServiceInterface FindService<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var services = GetServices<TServiceInterface>();
            return services.FirstOrDefault(predicate);
        }

        public IReadOnlyList<TServiceInterface> FindAllService<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var services = GetServices<TServiceInterface>();
            return services.Where(predicate).ToList();
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
