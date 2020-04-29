using System;
using System.Collections.Generic;
using ZyGames.Framework.Injection;

namespace ZyGames.Framework.Remote
{
    internal class ServiceDirectory
    {
        private readonly Dictionary<int, ServiceData> activations = new Dictionary<int, ServiceData>();

        public ServiceDirectory(IServiceProvider serviceProvider)
        {
            var serviceTypeManager = serviceProvider.GetRequiredService<ServiceTypeMetadataManager>();
            foreach (var serviceTypeData in serviceTypeManager.Metadatas)
            {
                var interfaceId = ServiceUtility.GetServiceInterfaceId(serviceTypeData.InterfaceType);
                var service = (IService)Activator.CreateInstance(serviceTypeData.ServiceType);
                var invoker = (IServiceMethodInvoker)Activator.CreateInstance(serviceTypeData.InvokerType);
                var activation = new ServiceData(service, invoker);
                activations[interfaceId] = activation;
            }
        }

        public ServiceData FindTarget(int interfaceId)
        {
            activations.TryGetValue(interfaceId, out ServiceData activition);
            return activition;
        }
    }
}
