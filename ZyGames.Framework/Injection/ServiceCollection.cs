using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ZyGames.Framework.Injection
{
    internal class ServiceCollection
    {
        private readonly List<ServiceDescriptor> items = new List<ServiceDescriptor>();

        public ServiceDescriptor GetServiceDescriptor(Type serviceType)
        {
            foreach (var descriptor in items)
            {
                if (descriptor.ServiceType == serviceType)
                {
                    return descriptor;
                }
            }
            //foreach (var descriptor in items)
            //{
            //    if (serviceType.IsAssignableFrom(descriptor.ServiceType))
            //    {
            //        return descriptor;
            //    }
            //}
            return null;
        }

        public ServiceDescriptor GetServiceDescriptor<TService>()
            where TService : class
        {
            return GetServiceDescriptor(typeof(TService));
        }

        public IServiceProvider Build()
        {
            return new ServiceProvider(this);
        }

        public void AddSingleton(Type serviceType, object instance)
        {
            var descriptor = GetServiceDescriptor(serviceType);
            if (descriptor == null)
            {
                descriptor = new ServiceDescriptor(serviceType, instance);
                items.Add(descriptor);
            }
        }

        public void AddSingleton(object instance)
        {
            var serviceType = instance.GetType();
            AddSingleton(serviceType, instance);
        }

        public void AddSingleton(Type serviceType, Type implementationType)
        {
            var descriptor = GetServiceDescriptor(serviceType);
            if (descriptor == null)
            {
                descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Singleton);
                items.Add(descriptor);
            }
        }

        public void AddSingleton(Type serviceType)
        {
            AddSingleton(serviceType, serviceType);
        }

        public void AddSingleton<TService>()
            where TService : class
        {
            AddSingleton(typeof(TService), typeof(TService));
        }

        public void AddSingleton<TService, TImplementation>()
             where TService : class
             where TImplementation : TService
        {
            AddSingleton(typeof(TService), typeof(TImplementation));
        }

        public void AddSingleton<TService>(object instance)
            where TService : class
        {
            AddSingleton(typeof(TService), instance);
        }

        public void AddSingleton<TService>(Func<IServiceProvider, TService> factory)
            where TService : class
        {
            var serviceType = typeof(TService);
            var descriptor = GetServiceDescriptor(serviceType);
            if (descriptor == null)
            {
                descriptor = new ServiceDescriptor(serviceType, factory, ServiceLifetime.Singleton);
                items.Add(descriptor);
            }
        }

        public void AddTransient(Type serviceType, Type implementationType)
        {
            var descriptor = GetServiceDescriptor(serviceType);
            if (descriptor == null)
            {
                descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
                items.Add(descriptor);
            }
        }

        public void AddTransient(Type serviceType)
        {
            AddTransient(serviceType, serviceType);
        }

        public void AddTransient<TService>()
            where TService : class
        {
            AddTransient(typeof(TService), typeof(TService));
        }

        public void AddTransient<TService, TImplementation>()
             where TService : class
             where TImplementation : TService
        {
            AddTransient(typeof(TService), typeof(TImplementation));
        }

        public void AddTransient<TService>(Func<IServiceProvider, TService> factory)
            where TService : class
        {
            var serviceType = typeof(TService);
            var descriptor = GetServiceDescriptor(serviceType);
            if (descriptor == null)
            {
                descriptor = new ServiceDescriptor(serviceType, factory, ServiceLifetime.Transient);
                items.Add(descriptor);
            }
        }

        class ServiceProvider : IServiceProvider
        {
            private readonly ServiceCollection collection;

            public ServiceProvider(ServiceCollection collection)
            {
                this.collection = collection;
            }

            private ConstructorInfo GetAvailableConstructor(Type implementationType)
            {
                var bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var constructors = implementationType.GetConstructors(bindingAttr);
                if (constructors.Length == 1)
                {
                    return constructors[0];
                }
                if (constructors.Length > 1)
                {
                    return implementationType.GetConstructor(bindingAttr, null, Type.EmptyTypes, null);
                }

                return null;
            }

            private Func<IServiceProvider, object> CreateImplementationFactory(ServiceDescriptor descriptor, List<Type> callings)
            {
                return new Func<IServiceProvider, object>((provider) =>
                {
                    var constructor = GetAvailableConstructor(descriptor.ImplementationType);
                    if (constructor == null)
                    {
                        throw new InvalidOperationException("not supported service constructor: " + descriptor.ServiceType.FullName);
                    }

                    var callingInitializing = false;
                    var parameters = constructor.GetParameters();
                    var arguments = new object[parameters.Length];
                    var serviceProviderType = typeof(IServiceProvider);
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        var parameterType = parameter.ParameterType;
                        if (parameterType == serviceProviderType)
                        {
                            arguments[i] = provider;
                            continue;
                        }
                        if (!callingInitializing)
                        {
                            callingInitializing = true;
                            if (callings == null) callings = new List<Type>() { descriptor.ServiceType };
                            else callings.Add(descriptor.ServiceType);
                        }
                        if (callings.Contains(parameterType))
                        {
                            var text = string.Join(Environment.NewLine + "-> ", callings.Select(p => p.FullName));
                            throw new InvalidOperationException("circular reference : " + text);
                        }

                        arguments[i] = GetServiceCore(parameter.ParameterType, new List<Type>(callings));
                    }

                    return constructor.Invoke(arguments);
                });
            }

            private object GetServiceCore(Type serviceType, List<Type> callings)
            {
                var descriptor = collection.GetServiceDescriptor(serviceType);
                if (descriptor == null)
                {
                    //service not found.
                    return null;
                }

                switch (descriptor.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        if (descriptor.ImplementationInstance == null)
                        {
                            lock (descriptor)
                            {
                                if (descriptor.ImplementationInstance == null)
                                {
                                    var implementationFactory = descriptor.ImplementationFactory ?? CreateImplementationFactory(descriptor, callings);
                                    descriptor.ImplementationInstance = implementationFactory(this);
                                }
                            }
                        }
                        return descriptor.ImplementationInstance;
                    case ServiceLifetime.Transient:
                        if (descriptor.ImplementationFactory == null)
                        {
                            lock (descriptor)
                            {
                                if (descriptor.ImplementationFactory == null)
                                {
                                    descriptor.ImplementationFactory = CreateImplementationFactory(descriptor, callings);
                                }
                            }
                        }
                        return descriptor.ImplementationFactory(this);
                    default:
                        throw new InvalidOperationException("not supported lifetime: " + descriptor.Lifetime.ToString());
                }
            }

            public object GetService(Type serviceType)
            {
                return GetServiceCore(serviceType, null);
            }
        }
    }
}
