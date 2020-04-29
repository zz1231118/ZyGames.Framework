using System;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote
{
    public class ServiceHostBuilder
    {
        private readonly ServiceTypeMetadataManager serviceTypeManager = new ServiceTypeMetadataManager();
        private readonly ServiceCollection collection = new ServiceCollection();
        private Binding binding;

        public ServiceHostBuilder ConfigureSingleBinding(Binding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            this.binding = binding;
            return this;
        }

        public ServiceHostBuilder ConfigureSingleBinding<T>()
            where T : Binding
        {
            binding = Activator.CreateInstance<T>();
            return this;
        }

        public ServiceHostBuilder AddSingleService(Type serviceType)
        {
            serviceTypeManager.AddServiceType(serviceType);
            return this;
        }

        public ServiceHostBuilder AddSingleService<T>()
        {
            var serviceType = typeof(T);
            serviceTypeManager.AddServiceType(serviceType);
            return this;
        }

        public ServiceHostBuilder ConfigureSingleOption(object option)
        {
            collection.AddSingleton(option);
            return this;
        }

        public ServiceHostBuilder ConfigureSingleOption<T>(Action<T> action = null)
            where T : class, new()
        {
            var option = Activator.CreateInstance<T>();
            action?.Invoke(option);
            collection.AddSingleton(option);
            return this;
        }

        public ServiceHost Build()
        {
            if (binding == null)
                throw new InvalidOperationException("binding is null.");

            collection.AddSingleton<Binding>(binding);
            collection.AddSingleton<ServiceTypeMetadataManager>(serviceTypeManager);
            collection.AddSingleton<ServiceDirectory>();
            collection.AddSingleton<MessageSerializer>();
            collection.AddSingleton<MessageDispatcher>();
            return new ServiceHost(collection.Build());
        }
    }
}
