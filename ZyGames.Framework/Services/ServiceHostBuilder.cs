using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.Injection;
using ZyGames.Framework.Services.Directory;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Membership;
using ZyGames.Framework.Services.Messaging;
using ZyGames.Framework.Services.Networking;
using ZyGames.Framework.Services.Options;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services
{
    public class ServiceHostBuilder
    {
        private readonly ContainerBuilder builder = new ContainerBuilder();
        private readonly List<ServiceDescriptor> serviceDescriptors = new List<ServiceDescriptor>();
        private readonly List<SystemTargetDescriptor> systemTargetDescriptors = new List<SystemTargetDescriptor>();

        public ServiceHostBuilder ConfigureOptions<T>(Action<T> initializer = null)
        {
            var options = Activator.CreateInstance<T>();
            initializer?.Invoke(options);
            builder.AddSingleton(options);
            return this;
        }

        public ServiceHostBuilder AddSystemTarget<ISystemTargetInterface, TSystemTarget>()
            where ISystemTargetInterface : ISystemTarget
            where TSystemTarget : SystemTarget, ISystemTargetInterface
        {
            var descriptor = new SystemTargetDescriptor();
            descriptor.InterfaceType = typeof(ISystemTargetInterface);
            descriptor.ImplementationType = typeof(TSystemTarget);
            systemTargetDescriptors.Add(descriptor);
            return this;
        }

        public ServiceHostBuilder AddSystemTarget<ISystemTargetInterface, TSystemTarget, TOptions>(Action<TOptions> initializer = null)
            where ISystemTargetInterface : ISystemTarget
            where TSystemTarget : SystemTarget, ISystemTargetInterface, IOptions<TOptions>
        {
            var descriptor = new SystemTargetDescriptor();
            descriptor.InterfaceType = typeof(ISystemTargetInterface);
            descriptor.ImplementationType = typeof(TSystemTarget);
            systemTargetDescriptors.Add(descriptor);

            var options = Activator.CreateInstance<TOptions>();
            initializer?.Invoke(options);
            builder.AddSingleton(options);
            return this;
        }

        public ServiceHostBuilder AddSystemTarget<ISystemTargetInterface, TSystemTarget, TOptions>(TOptions options)
            where ISystemTargetInterface : ISystemTarget
            where TSystemTarget : SystemTarget, ISystemTargetInterface, IOptions<TOptions>
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var descriptor = new SystemTargetDescriptor();
            descriptor.InterfaceType = typeof(ISystemTargetInterface);
            descriptor.ImplementationType = typeof(TSystemTarget);
            systemTargetDescriptors.Add(descriptor);

            builder.AddSingleton(options);
            return this;
        }

        public ServiceHostBuilder AddService<TServiceInterface, TService>(Identity identity = null, object metadata = null)
            where TServiceInterface : IService
            where TService : Service, TServiceInterface
        {
            var descriptor = new ServiceDescriptor();
            descriptor.InterfaceType = typeof(TServiceInterface);
            descriptor.ImplementationType = typeof(TService);
            descriptor.Identity = identity;
            descriptor.Metadata = metadata;
            serviceDescriptors.Add(descriptor);
            return this;
        }

        public ServiceHostBuilder AddComponent<TService, TImplementation>()
            where TService : class
            where TImplementation : TService
        {
            builder.AddSingleton<TService, TImplementation>();
            return this;
        }

        public ServiceHostBuilder AddComponent<TService, TImplementation>(Action<TImplementation> initializer)
            where TService : class
            where TImplementation : TService
        {
            if (initializer == null)
                throw new ArgumentNullException(nameof(initializer));

            var component = Activator.CreateInstance<TImplementation>();
            initializer(component);
            builder.AddSingleton<TService>(component);
            return this;
        }

        public ServiceHostBuilder AddComponent<TService>(object instance)
            where TService : class
        {
            builder.AddSingleton<TService>(instance);
            return this;
        }

        public ServiceHostBuilder AddComponent<TService>(Func<IContainer, TService> factory)
            where TService : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            builder.AddSingleton<TService>(factory);
            return this;
        }

        public ServiceHost Build()
        {
            builder.AddSingleton<IMessageSerializer, MessageProtobufSerializer>();
            builder.AddSingleton<MessageCenter>();
            builder.AddSingleton<AddressableDirectory>();
            builder.AddSingleton<ActivationDirectory>();
            builder.AddSingleton<AddressableTypeManager>();
            builder.AddSingleton<IServiceFactory>(p =>
            {
                var serviceFactory = new ServiceFactory(p);
                foreach (var descriptor in systemTargetDescriptors)
                {
                    serviceFactory.NewSystemTarget(descriptor.ImplementationType, descriptor.InterfaceType);
                }
                foreach (var descriptor in serviceDescriptors)
                {
                    serviceFactory.NewService(descriptor.ImplementationType, descriptor.InterfaceType, descriptor.Identity, descriptor.Metadata);
                }
                return serviceFactory;
            });
            builder.AddSingleton<ReferenceRuntime>();
            builder.AddSingleton<MembershipManager>();
            builder.AddSingleton<BinarySerializer>();
            builder.AddSingleton<IConnectionManager, ConnectionManager2>();
            builder.AddSingleton<IServiceHostLifecycle, ServiceHostLifecycle>();
            builder.AddSingleton<IDirectoryLifecycle, DirectoryLifecycle>();
            builder.AddSingleton<IMembershipLifecycle, MembershipLifecycle>();
            builder.AddSingleton<ServiceHost>();
            builder.TryAddSingleton<TaskScheduler>(TaskScheduler.Default);
            builder.EnableAutowired();
            var container = builder.Build();
            return container.Required<ServiceHost>();
        }

        abstract class AddressableDescriptor
        {
            public Type InterfaceType;
            public Type ImplementationType;
        }

        sealed class SystemTargetDescriptor : AddressableDescriptor
        { }

        sealed class ServiceDescriptor : AddressableDescriptor
        {
            public Identity Identity;
            public object Metadata;
        }
    }
}
