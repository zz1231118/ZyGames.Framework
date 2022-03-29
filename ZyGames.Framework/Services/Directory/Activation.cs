using System;
using ZyGames.Framework.Services.Messaging;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services.Directory
{
    internal sealed class Activation
    {
        private readonly Mailbox mailbox = new Mailbox();
        private readonly Addressable addressable;
        private readonly IMethodInvoker methodInvoker;
        private readonly Type interfaceType;
        private readonly Priority priority;

        public Activation(Addressable addressable, IMethodInvoker methodInvoker, Type interfaceType, Priority priority)
        {
            this.addressable = addressable;
            this.methodInvoker = methodInvoker;
            this.interfaceType = interfaceType;
            this.priority = priority;
        }

        public Priority Priority => priority;

        public Identity Identity => addressable.Identity;

        public IAddressable Addressable => addressable;

        public Type InterfaceType => interfaceType;

        public Mailbox Mailbox => mailbox;

        public string GetMethodName(InvokeMethodRequest request, bool throwOnError)
        {
            try
            {
                return methodInvoker.GetMethodName(request);
            }
            catch
            {
                if (throwOnError) throw;
                else return null;
            }
        }

        public object Invoke(InvokeMethodRequest request)
        {
            return methodInvoker.Invoke(addressable, request);
        }

        public void Start()
        {
            InvokerContext.Caller = addressable;
            try
            {
                addressable.Start();
            }
            finally
            {
                InvokerContext.Caller = null;
            }
        }

        public void Stop()
        {
            InvokerContext.Caller = addressable;
            try
            {
                addressable.Stop();
            }
            finally
            {
                InvokerContext.Caller = null;
            }
        }
    }
}
