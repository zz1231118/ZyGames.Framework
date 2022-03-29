using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Framework.Injection;
using Framework.Log;
using Framework.Threading;
using ZyGames.Framework.Services.Directory;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Messaging;
using ZyGames.Framework.Services.Networking;
using ZyGames.Framework.Services.Options;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services
{
    internal class MessageCenter
    {
        private readonly ILogger logger = Logger.GetLogger<MessageCenter>();
        private readonly InterfaceToImplementationMapping interfaceToImplementationMapping = new InterfaceToImplementationMapping();
        private readonly ConcurrentDictionary<Guid, AwaitableCompletionSource<object>> contexts = new ConcurrentDictionary<Guid, AwaitableCompletionSource<object>>();
        private readonly IContainer container;
        private readonly ActivationDirectory activationDirectory;
        private readonly IConnectionManager connectionManager;
        private readonly GatewayMembershipServiceOptions gatewayMembershipServiceOptions;
        private readonly ClusterMembershipServiceOptions clusterMembershipServiceOptions;
        private readonly IServiceHostLifecycle hostingLifecycle;
        private readonly TaskScheduler taskScheduler;
        private readonly CancellationToken hostingCancellationToken;
        private readonly Action<object> dispatchHandle;
        private List<IServiceCallFilter> serviceCallFilters;

        public MessageCenter(IContainer container)
        {
            this.container = container;
            this.activationDirectory = container.Required<ActivationDirectory>();
            this.connectionManager = container.Required<IConnectionManager>();
            this.gatewayMembershipServiceOptions = container.Optional<GatewayMembershipServiceOptions>();
            this.clusterMembershipServiceOptions = container.Optional<ClusterMembershipServiceOptions>();
            this.hostingLifecycle = container.Required<IServiceHostLifecycle>();
            this.taskScheduler = container.Required<TaskScheduler>();
            this.hostingCancellationToken = hostingLifecycle.Token;
            this.dispatchHandle = new Action<object>(Dispatch);
        }

        private List<IServiceCallFilter> ServiceCallFilters => serviceCallFilters ??= container.Repeated<IServiceCallFilter>().ToList();

        private void Dispatch(object obj)
        {
            if (hostingCancellationToken.IsCancellationRequested)
            {
                //hosting stopped
                return;
            }

            var invoker = (ServiceRequestInvoker)obj;
            var message = invoker.Message;
            var activation = invoker.Activation;

            try
            {
                try
                {
                    invoker.Invoke();
                }
                catch (Exception ex)
                {
                    var serviceType = activation.InterfaceType.FullName;
                    var method = invoker.InterfaceMethod?.Name ?? "unknown";
                    logger.Warn("Service:{{Type:{0} Identity:{1}}} invoke method:{2} exception:{3}", serviceType, activation.Identity, method, ex);

                    var exception = new ServiceRequestException(ex.Message);
                    var faultedMessage = message.CreateErrorMessage(exception);
                    SendMessage(faultedMessage);
                    return;
                }
                if (message.Direction == Message.Directions.Request)
                {
                    var responseMessage = message.CreateResponseMessage(invoker.Result);
                    SendMessage(responseMessage);
                }
            }
            catch (Exception ex)
            {
                var serviceType = activation.InterfaceType.FullName;
                var method = invoker.InterfaceMethod?.Name ?? "unknown";
                logger.Error("Dispatch Addressable:{{Type:{0},Identity:{1}}} method:{2} exception:{3}", serviceType, activation.Identity, method, ex);
            }
            finally
            {
                activation.Mailbox.Decrement();
            }
        }

        private void OnReceivedMessage(Message message)
        {
            var activation = activationDirectory.FindTarget(message.TargetId);
            if (activation == null)
            {
                var exception = new ServiceNotFoundException(message.TargetId);
                var faultedMessage = message.CreateErrorMessage(exception);
                SendMessage(faultedMessage);
                return;
            }
            var membershipServiceOptions = GetMembershipServiceOptions(message.TargetSilo);
            if (membershipServiceOptions.Overloaded > 0 && activation.Mailbox.Count >= membershipServiceOptions.Overloaded)
            {
                var faultedMessage = message.CreateRejectionMessage(Message.RejectionTypes.Overloaded);
                SendMessage(faultedMessage);
                return;
            }

            activation.Mailbox.Increment();
            var serviceRequestInvoker = new ServiceRequestInvoker(activation, message, ServiceCallFilters, interfaceToImplementationMapping);
            var taskCreationOptions = TaskCreationOptions.PreferFairness | TaskCreationOptions.RunContinuationsAsynchronously;
            var task = new Task(dispatchHandle, serviceRequestInvoker, hostingLifecycle.Token, taskCreationOptions);
            task.Start(taskScheduler);
        }

        private bool TrySendLocal(Message message)
        {
            if (message.TargetSilo == gatewayMembershipServiceOptions?.OutsideAddress ||
                message.TargetSilo == clusterMembershipServiceOptions?.OutsideAddress)
            {
                ReceiveMessage(message);
                return true;
            }

            return false;
        }

        private ConnectionListenerOptions GetMembershipServiceOptions(Address address)
        {
            if (clusterMembershipServiceOptions == null) return gatewayMembershipServiceOptions;
            if (gatewayMembershipServiceOptions == null) return clusterMembershipServiceOptions;
            if (address == null) return gatewayMembershipServiceOptions;

            return gatewayMembershipServiceOptions.OutsideAddress == address
                ? gatewayMembershipServiceOptions
                : clusterMembershipServiceOptions;
        }

        public void ReceiveMessage(Message message)
        {
            switch (message.Direction)
            {
                case Message.Directions.Response:
                    if (contexts.TryGetValue(message.Id, out var context))
                    {
                        switch (message.Result)
                        {
                            case Message.ResponseTypes.Success:
                                context.SetResult(message.Body);
                                break;
                            case Message.ResponseTypes.Error:
                                context.SetException((Exception)message.Body);
                                break;
                            case Message.ResponseTypes.Rejection:
                                context.SetException(new ServiceRejectionException(message.RejectionType));
                                break;
                        }
                    }
                    break;
                default:
                    OnReceivedMessage(message);
                    break;
            }
        }

        public void SendMessage(Message message)
        {
            if (message.SendingSilo == null)
            {
                var membershipServiceOptions = GetMembershipServiceOptions(message.SendingSilo);
                message.SendingSilo = membershipServiceOptions.OutsideAddress;
            }
            if (!TrySendLocal(message))
            {
                var connection = connectionManager.Allocate(message.TargetSilo);
                try
                {
                    connection.SendMessage(message);
                }
                finally
                {
                    connectionManager.Return(message.TargetSilo, connection);
                }
            }
        }

        public object SendRequest(Message message, int timeoutMills)
        {
            var context = new AwaitableCompletionSource<object>();
            contexts[message.Id] = context;

            try
            {
                SendMessage(message);
                var membershipServiceOptions = GetMembershipServiceOptions(message.SendingSilo);
                var requestTimeout = timeoutMills == Constants.RequestTimeout.None
                    ? membershipServiceOptions.RequestTimeout
                    : timeoutMills == Constants.RequestTimeout.Infinite
                        ? Timeout.InfiniteTimeSpan
                        : TimeSpan.FromMilliseconds(timeoutMills);
                return context.GetResult(requestTimeout);
            }
            finally
            {
                contexts.TryRemove(message.Id, out _);
                context.Dispose();
            }
        }

        sealed class ServiceRequestInvoker : IServiceCallContext
        {
            private readonly Activation activation;
            private readonly Message message;
            private readonly InvokeMethodRequest request;
            private readonly List<IServiceCallFilter> filters;
            private readonly InterfaceToImplementationMapping interfaceToImplementationMapping;
            private int stage;

            public ServiceRequestInvoker(Activation activation, Message message, List<IServiceCallFilter> filters, InterfaceToImplementationMapping interfaceToImplementationMapping)
            {
                this.activation = activation;
                this.message = message;
                this.request = (InvokeMethodRequest)message.Body;
                this.filters = filters;
                this.interfaceToImplementationMapping = interfaceToImplementationMapping;
            }

            public Activation Activation => activation;

            public Message Message => message;

            public IAddressable Service => Activation.Addressable;

            public MethodInfo InterfaceMethod => GetMethodEntry().InterfaceMethod;

            public MethodInfo ImplementationMethod => GetMethodEntry().ImplementationMethod;

            public object[] Arguments => request.Arguments;

            public object Result { get; set; }

            private InterfaceToImplementationMapping.Entry GetMethodEntry()
            {
                var interfaceType = this.activation.InterfaceType;
                var implementationType = activation.Addressable.GetType();
                var implementationMap = interfaceToImplementationMapping.Gain(implementationType, interfaceType);

                implementationMap.TryGetValue(request.MethodId, out var method);
                return method;
            }

            public void Invoke()
            {
                if (stage < filters.Count)
                {
                    var filter = filters[stage++];
                    filter.Invoke(this);
                    return;
                }

                switch (message.Direction)
                {
                    case Message.Directions.Request:
                        InvokerContext.Caller = activation.Addressable;

                        try
                        {
                            Result = activation.Invoke(request);
                        }
                        finally
                        {
                            InvokerContext.Caller = null;
                        }
                        break;
                    case Message.Directions.OneWay:
                        InvokerContext.Caller = activation.Addressable;

                        try
                        {
                            activation.Invoke(request);
                        }
                        finally
                        {
                            InvokerContext.Caller = null;
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"invalid direction:{message.Direction}. service:{message.TargetId}");
                }
            }
        }
    }
}