using System;

namespace ZyGames.Framework.Injection
{
    public static class ServiceExtensions
    {
        public static T GetService<T>(this IServiceProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return (T)provider.GetService(typeof(T));
        }

        public static object GetRequiredService(this IServiceProvider provider, Type serviceType)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            var service = provider.GetService(serviceType);
            if (service == null)
            {
                throw new ServiceNotFoundException(serviceType.FullName);
            }

            return service;
        }

        public static T GetRequiredService<T>(this IServiceProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var serviceType = typeof(T);
            var service = provider.GetService(serviceType);
            if (service == null)
            {
                throw new ServiceNotFoundException(serviceType.FullName);
            }

            return (T)service;
        }
    }
}
