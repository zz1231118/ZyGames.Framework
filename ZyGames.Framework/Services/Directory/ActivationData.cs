using System;
using System.Threading;
using ZyGames.Framework.Services.Messaging;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services.Directory
{
    internal sealed class ActivationData
    {
        private const int NoneSentinel = 0;
        private const int ActiveSentinel = 1;

        private readonly Addressable addressable;
        private readonly IMethodInvoker methodInvoker;
        private readonly Type interfaceType;
        private readonly Priority priority;
        private readonly InvokeContextCategory invokeContextCategory;
        private readonly Mailbox mailbox = new Mailbox();
        private int isInSending;
        private int inFlightCount;

        public ActivationData(Addressable addressable, IMethodInvoker methodInvoker, Type interfaceType, Priority priority, InvokeContextCategory invokeContextCategory)
        {
            this.addressable = addressable;
            this.methodInvoker = methodInvoker;
            this.interfaceType = interfaceType;
            this.priority = priority;
            this.invokeContextCategory = invokeContextCategory;
        }

        public Priority Priority => priority;

        public InvokeContextCategory InvokeContextCategory => invokeContextCategory;

        public Identity Identity => addressable.Identity;

        public IAddressable Addressable => addressable;

        public Type InterfaceType => interfaceType;

        public Mailbox Mailbox => mailbox;

        public TimeSpan Consuming { get; set; }

        public int InFlightCount => inFlightCount;

        public string GetMethodName(Message message, bool throwOnError)
        {
            try
            {
                var request = (InvokeMethodRequest)message.BodyObject;
                return methodInvoker.GetMethodName(request);
            }
            catch
            {
                if (throwOnError) throw;
                else return null;
            }
        }

        public bool DirectSendOrEnqueue(Message message)
        {
            lock (this)
            {
                mailbox.Enqueue(message);
                IncrementInFlightCount();
                return Interlocked.CompareExchange(ref isInSending, ActiveSentinel, NoneSentinel) == NoneSentinel;
            }
        }

        public bool TryDequeueOrReset(out Message message)
        {
            lock (this)
            {
                if (mailbox.TryDequeue(out message))
                {
                    DecrementInFlightCount();
                    return true;
                }

                Interlocked.Exchange(ref isInSending, NoneSentinel);
                return false;
            }
        }

        public object Invoke(Message message)
        {
            var request = (InvokeMethodRequest)message.BodyObject;
            return methodInvoker.Invoke(addressable, request);
        }

        public void IncrementInFlightCount()
        {
            Interlocked.Increment(ref inFlightCount);
        }

        public void DecrementInFlightCount()
        {
            Interlocked.Decrement(ref inFlightCount);
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
