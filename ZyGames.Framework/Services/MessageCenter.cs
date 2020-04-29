using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Framework.Log;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Directory;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Messaging;
using ZyGames.Framework.Services.Options;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services
{
    internal class MessageCenter
    {
        private readonly ILogger logger = Logger.GetLogger<MessageCenter>();
        private readonly ConcurrentDictionary<Guid, CompletionSource<object>> requestContexts = new ConcurrentDictionary<Guid, CompletionSource<object>>();
        private readonly ActivationDirectory activationDirectory;
        private readonly ConnectionManager connectionManager;
        private readonly GatewayMembershipServiceOptions gatewayMembershipServiceOptions;
        private readonly ClusterMembershipServiceOptions clusterMembershipServiceOptions;
        private readonly IServiceHostLifecycle hostingLifecycle;
        private readonly CancellationToken cancellationToken;

        public MessageCenter(IServiceProvider serviceProvider)
        {
            this.activationDirectory = serviceProvider.GetRequiredService<ActivationDirectory>();
            this.connectionManager = serviceProvider.GetRequiredService<ConnectionManager>();
            this.gatewayMembershipServiceOptions = serviceProvider.GetService<GatewayMembershipServiceOptions>();
            this.clusterMembershipServiceOptions = serviceProvider.GetService<ClusterMembershipServiceOptions>();
            this.hostingLifecycle = serviceProvider.GetRequiredService<IServiceHostLifecycle>();
            this.cancellationToken = hostingLifecycle.Token;
        }

        private void Execute(ActivationData activation, Message message)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                //application closed
                return;
            }
            switch (message.Direction)
            {
                case Message.Directions.Request:
                    {
                        InvokerContext.Caller = activation.Addressable;

                        try
                        {
                            var result = activation.Invoke(message);
                            var response = message.CreateResponseMessage(result);
                            SendMessage(response);
                        }
                        catch (Exception ex)
                        {
                            var faultedMessage = message.CreateErrorMessage(ex);
                            SendMessage(faultedMessage);

                            var method = activation.GetMethodName(message, false);
                            var serviceType = activation.InterfaceType.FullName;
                            logger.Warn("Service:{{Type:{0} Identity:{1}}} invoke method:{2} exception:{3}", serviceType, activation.Identity, method ?? "unknown", ex);
                        }
                        finally
                        {
                            InvokerContext.Caller = null;
                        }
                    }
                    break;
                case Message.Directions.OneWay:
                    {
                        InvokerContext.Caller = activation.Addressable;

                        try
                        {
                            activation.Invoke(message);
                        }
                        catch (Exception ex)
                        {
                            var method = activation.GetMethodName(message, false);
                            var serviceType = activation.InterfaceType.FullName;
                            logger.Warn("Service:{{Type:{0} Identity:{1}}} invoke method:{2} exception:{3}", serviceType, activation.Identity, method ?? "unknown", ex);
                        }
                        finally
                        {
                            InvokerContext.Caller = null;
                        }
                    }
                    break;
                default:
                    {
                        var exception = new InvalidOperationException(string.Format("invalid direction:{0}. service:{1}", message.Direction, message.TargetId));
                        var faultedMessage = message.CreateErrorMessage(exception);
                        SendMessage(faultedMessage);
                    }
                    break;
            }
        }

        private void Dispatch(object obj)
        {
            ActivationData activation = (ActivationData)obj;
            while (!cancellationToken.IsCancellationRequested && activation.TryDequeueOrReset(out Message message))
            {
                Execute(activation, message);
            }
        }

        private void OnReceivedMessage(Message message)
        {
            var target = activationDirectory.FindTarget(message.TargetId);
            if (target == null)
            {
                var exception = new ServiceNotFoundException(message.TargetId);
                var faultedMessage = message.CreateErrorMessage(exception);
                SendMessage(faultedMessage);
                return;
            }
            var membershipServiceOptions = GetMembershipServiceOptions(message.TargetSlio);
            if (membershipServiceOptions.Overloaded > 0 && target.InFlightCount >= membershipServiceOptions.Overloaded)
            {
                var faultedMessage = message.CreateRejectionMessage(Message.RejectionTypes.Overloaded);
                SendMessage(faultedMessage);
                return;
            }
            switch (target.InvokeContextCategory)
            {
                case InvokeContextCategory.Multi:
                    Task.Factory.StartNew(new Action(() =>
                    {
                        target.IncrementInFlightCount();
                        Execute(target, message);
                        target.DecrementInFlightCount();
                    }));
                    break;
                case InvokeContextCategory.Single:
                    if (target.DirectSendOrEnqueue(message))
                    {
                        Task.Factory.StartNew(new Action<object>(Dispatch), target);
                    }
                    break;
            }
        }

        private bool TrySendLocal(Message message)
        {
            if (message.TargetSlio == gatewayMembershipServiceOptions?.OutsideAddress ||
                message.TargetSlio == clusterMembershipServiceOptions?.OutsideAddress)
            {
                ReceiveMessage(message);
                return true;
            }

            return false;
        }

        private ConnectionListenerOptions GetMembershipServiceOptions(SlioAddress address)
        {
            if (clusterMembershipServiceOptions == null) return gatewayMembershipServiceOptions;
            if (gatewayMembershipServiceOptions == null) return clusterMembershipServiceOptions;
            if (address == null) return gatewayMembershipServiceOptions;

            return gatewayMembershipServiceOptions.OutsideAddress == address
                ? (ConnectionListenerOptions)gatewayMembershipServiceOptions
                : clusterMembershipServiceOptions;
        }

        public void ReceiveMessage(Message message)
        {
            switch (message.Direction)
            {
                case Message.Directions.Response:
                    if (requestContexts.TryGetValue(message.Guid, out CompletionSource<object> context))
                    {
                        switch (message.Result)
                        {
                            case Message.ResponseTypes.Success:
                                context.SetResult(message.BodyObject);
                                break;
                            case Message.ResponseTypes.Error:
                                context.SetException((Exception)message.BodyObject);
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
            if (message.SendingSlio == null)
            {
                var membershipServiceOptions = GetMembershipServiceOptions(message.SendingSlio);
                message.SendingSlio = membershipServiceOptions.OutsideAddress;
            }
            if (!TrySendLocal(message))
            {
                var connection = connectionManager.GetConnection(message.TargetSlio);
                connection.SendMessage(message);
            }
        }

        public object SendRequest(Message message)
        {
            var context = new CompletionSource<object>();
            requestContexts[message.Guid] = context;
            try
            {
                SendMessage(message);
                var membershipServiceOptions = GetMembershipServiceOptions(message.SendingSlio);
                return context.GetResult(membershipServiceOptions.RequestTimeout);
            }
            finally
            {
                requestContexts.TryRemove(message.Guid, out _);
                context.Dispose();
            }
        }
    }
}