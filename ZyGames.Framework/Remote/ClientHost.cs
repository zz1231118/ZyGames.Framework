using System;
using System.Reflection;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Remote.Networking;

namespace ZyGames.Framework.Remote
{
    public sealed class ClientHost : IDisposable
    {
        private readonly Binding binding;
        private readonly ServiceTypeDataManager serviceTypeManager;
        private readonly ClientRuntime clientRuntime;
        private readonly ServiceReferenceRuntime serviceReferenceRuntime;
        private bool isDisposed;

        internal ClientHost(IServiceProvider serviceProvider)
        {
            this.binding = serviceProvider.GetRequiredService<Binding>();
            this.serviceTypeManager = serviceProvider.GetRequiredService<ServiceTypeDataManager>();
            this.clientRuntime = binding.CreateClientRuntime(serviceProvider);
            this.serviceReferenceRuntime = new ServiceReferenceRuntime(serviceProvider, clientRuntime);
        }

        private void CheckDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                try
                {
                    clientRuntime.Dispose();
                }
                finally
                {
                    isDisposed = true;
                }
            }
        }

        public T GetService<T>()
            where T : class
        {
            CheckDisposed();

            var serviceInterfaceType = typeof(T);
            if (!serviceInterfaceType.IsInterface)
            {
                throw new ArgumentException("not interface type");
            }
            var serviceContract = serviceInterfaceType.GetCustomAttribute<ServiceContractAttribute>();
            if (serviceContract == null)
            {
                throw new ArgumentException("not contract interface type.");
            }
            var bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            foreach (var method in serviceInterfaceType.GetMethods(bindingAttr))
            {
                var operationContract = method.GetCustomAttribute<OperationContractAttribute>();
                if (operationContract != null && operationContract.Options.HasFlag(InvokeMethodOptions.OneWay) && method.ReturnType != typeof(void))
                {
                    throw new ArgumentException(string.Format("invalid method:{0} options:{1}", method.Name, nameof(InvokeMethodOptions.OneWay)));
                }
            }

            var serviceTypeData = serviceTypeManager.GetServiceTypeData(serviceInterfaceType);
            var referenceCreator = serviceTypeData.ReferenceCreator;
            return (T)referenceCreator(serviceReferenceRuntime);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
