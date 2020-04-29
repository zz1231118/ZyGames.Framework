using System;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote
{
    public class ClientBuilder
    {
        private Binding binding;
        private readonly ServiceCollection collection = new ServiceCollection();

        public ClientBuilder ConfigureSingleBinding(Binding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            this.binding = binding;
            return this;
        }

        public ClientBuilder ConfigureSingleBinding<T>()
            where T : Binding
        {
            binding = Activator.CreateInstance<T>();
            return this;
        }

        public ClientBuilder ConfigureSingleOption(object option)
        {
            collection.AddSingleton(option);
            return this;
        }

        public ClientBuilder ConfigureSingleOption<T>(Action<T> action = null)
            where T : class, new()
        {
            var option = Activator.CreateInstance<T>();
            action?.Invoke(option);
            collection.AddSingleton(option);
            return this;
        }

        public ClientHost Build()
        {
            if (binding == null)
                throw new InvalidOperationException("binding is null.");

            collection.AddSingleton<Binding>(binding);
            collection.AddSingleton<MessageSerializer>();
            collection.AddSingleton<ServiceTypeDataManager>();
            return new ClientHost(collection.Build());
        }
    }
}
