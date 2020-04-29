using System;
using System.Collections.Generic;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Directory;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Membership;
using ZyGames.Framework.Services.Messaging;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services
{
    public class ServiceHostBuilder
    {
        private readonly ServiceCollection serviceCollection = new ServiceCollection();
        private readonly List<ServiceDescriptor> serviceDescriptors = new List<ServiceDescriptor>();
        private readonly List<SystemTargetDescriptor> systemTargetDescriptors = new List<SystemTargetDescriptor>();

        public ServiceHostBuilder ConfigureOptions<T>(Action<T> action = null)
        {
            var option = Activator.CreateInstance<T>();
            action?.Invoke(option);
            serviceCollection.AddSingleton(option);
            return this;
        }

        public ServiceHostBuilder AddSystemTarget<ISystemTargetInterface, TSystemTarget>()
            where ISystemTargetInterface : ISystemTarget
            where TSystemTarget : SystemTarget, ISystemTargetInterface
        {
            var descriptor = new SystemTargetDescriptor();
            descriptor.SystemTargetInterfaceType = typeof(ISystemTargetInterface);
            descriptor.SystemTargetType = typeof(TSystemTarget);
            systemTargetDescriptors.Add(descriptor);
            return this;
        }

        public ServiceHostBuilder AddService<TServiceInterface, TService>(Identity identity = null, object metadata = null)
            where TServiceInterface : IService
            where TService : Service, TServiceInterface
        {
            var descriptor = new ServiceDescriptor();
            descriptor.ServiceInterfaceType = typeof(TServiceInterface);
            descriptor.ServiceType = typeof(TService);
            descriptor.Identity = identity;
            descriptor.Metadata = metadata;
            serviceDescriptors.Add(descriptor);
            return this;
        }

        public ServiceHost Build()
        {
            serviceCollection.AddSingleton<IMessageSerializer, MessageSerializer>();
            serviceCollection.AddSingleton<MessageCenter>();
            serviceCollection.AddSingleton<AddressableDirectory>();
            serviceCollection.AddSingleton<ActivationDirectory>();
            serviceCollection.AddSingleton<AddressableTypeManager>();
            serviceCollection.AddSingleton<ServiceFactory>();
            serviceCollection.AddSingleton<ReferenceRuntime>();
            serviceCollection.AddSingleton<ConnectionManager>();
            serviceCollection.AddSingleton<MembershipManager>();
            serviceCollection.AddSingleton<BinarySerializer>();
            serviceCollection.AddSingleton<IServiceHostLifecycle, ServiceHostLifecycle>();
            serviceCollection.AddSingleton<IDirectoryLifecycle, DirectoryLifecycle>();
            serviceCollection.AddSingleton<IMembershipLifecycle, MembershipLifecycle>();
            serviceCollection.AddSingleton<ServiceHost>();

            var serviceProvider = serviceCollection.Build();
            var serviceFactory = serviceProvider.GetRequiredService<ServiceFactory>();
            foreach (var descriptor in systemTargetDescriptors)
            {
                serviceFactory.NewSystemTarget(descriptor.SystemTargetType, descriptor.SystemTargetInterfaceType);
            }
            foreach (var descriptor in serviceDescriptors)
            {
                serviceFactory.NewService(descriptor.ServiceType, descriptor.ServiceInterfaceType, descriptor.Identity, descriptor.Metadata);
            }

            return serviceProvider.GetRequiredService<ServiceHost>();
        }

        class ServiceDescriptor
        {
            public Type ServiceInterfaceType;
            public Type ServiceType;
            public Identity Identity;
            public object Metadata;
        }
        class SystemTargetDescriptor
        {
            public Type SystemTargetInterfaceType;
            public Type SystemTargetType;
        }
    }
}
