using System;

namespace ZyGames.Framework.Injection
{
    internal class ServiceDescriptor
    {
        private readonly Type serviceType;
        private readonly ServiceLifetime lifetime;
        private Type implementationType;
        private object implementationInstance;
        private Func<IServiceProvider, object> implementationFactory;

        private ServiceDescriptor(Type serviceType, ServiceLifetime lifetime)
        {
            this.serviceType = serviceType;
            this.lifetime = lifetime;
        }

        public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
            : this(serviceType, lifetime)
        {
            this.implementationType = implementationType;
        }

        public ServiceDescriptor(Type serviceType, object implementationInstance)
            : this(serviceType, ServiceLifetime.Singleton)
        {
            this.implementationInstance = implementationInstance;
        }

        public ServiceDescriptor(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime)
            : this(serviceType, lifetime)
        {
            this.implementationFactory = implementationFactory;
        }

        public ServiceLifetime Lifetime => lifetime;

        public Type ServiceType => serviceType;

        public Type ImplementationType
        {
            get
            {
                if (implementationType == null)
                {
                    if (implementationInstance != null)
                    {
                        implementationType = implementationInstance.GetType();
                    }
                    else if (implementationFactory != null)
                    {
                        implementationType = implementationFactory.GetType().GenericTypeArguments[1];
                    }
                }
                return implementationType;
            }
        }

        public object ImplementationInstance
        {
            get => implementationInstance;
            set => implementationInstance = value;
        }

        public Func<IServiceProvider, object> ImplementationFactory
        {
            get => implementationFactory;
            set => implementationFactory = value;
        }
    }
}
