using System;
using System.Net;
using System.Threading;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote.Networking
{
    public class BasicHttpBinding : Binding
    {
        public override ConnectionListener CreateConnectionListener(IServiceProvider serviceProvider)
        {
            return new HttpConnectionListener(serviceProvider);
        }

        public override ClientRuntime CreateClientRuntime(IServiceProvider serviceProvider)
        {
            return new HttpClientRuntime(serviceProvider);
        }

        class HttpConnection : Connection
        {
            private readonly HttpListenerContext httpContext;
            private readonly MessageSerializer serializer;

            public HttpConnection(HttpListenerContext httpContext, MessageSerializer serializer)
            {
                this.httpContext = httpContext;
                this.serializer = serializer;
            }

            public override void SendMessage(Message message)
            {
                var bytes = serializer.Serialize(message);
                httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                httpContext.Response.ContentLength64 = bytes.Length;
                httpContext.Response.ContentType = "application/octet-stream";
                httpContext.Response.OutputStream.Write(bytes, 0, bytes.Length);
            }
        }

        class HttpConnectionListener : ConnectionListener
        {
            private readonly BasicHttpBindingOptions bindingOptions;
            private readonly MessageSerializer serializer;
            private readonly MessageDispatcher dispatcher;
            private HttpListener httpListener;

            public HttpConnectionListener(IServiceProvider serviceProvider)
            {
                this.bindingOptions = serviceProvider.GetRequiredService<BasicHttpBindingOptions>();
                this.serializer = serviceProvider.GetRequiredService<MessageSerializer>();
                this.dispatcher = serviceProvider.GetRequiredService<MessageDispatcher>();
            }

            private void PostAccept()
            {
                httpListener.BeginGetContext(new AsyncCallback(ProcessAccept), httpListener);
            }

            private void ProcessAccept(IAsyncResult ar)
            {
                var httpListener = (HttpListener)ar.AsyncState;
                HttpListenerContext context;

                try
                {
                    context = httpListener.EndGetContext(ar);
                }
                catch (ObjectDisposedException)
                {
                    //被释放了
                    return;
                }

                ThreadPool.QueueUserWorkItem(new WaitCallback(SetupContext), context);
                PostAccept();
            }

            private void SetupContext(object obj)
            {
                try
                {
                    var httpContext = (HttpListenerContext)obj;
                    var bytes = new byte[httpContext.Request.ContentLength64];
                    httpContext.Request.InputStream.Read(bytes, 0, bytes.Length);

                    var message = serializer.Deserialize(bytes);
                    var connection = new HttpConnection(httpContext, serializer);

                    dispatcher.Dispatch(connection, message);
                    httpContext.Response.Close();
                }
                catch (ObjectDisposedException)
                { }
                catch (Exception)
                { }
            }

            protected override void OnStart()
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(bindingOptions.Url);
                httpListener.Start();

                PostAccept();
            }

            protected override void OnStop()
            {
                var listener = Interlocked.Exchange(ref httpListener, null);
                if (listener != null)
                {
                    listener.Stop();
                }
            }

            class Session
            {
                public Guid Guid { get; set; }


            }
        }

        class HttpClientRuntime : ClientRuntime
        {
            public const string SessionCookieKey = "SESSION";

            private readonly BasicHttpBindingOptions bindingOption;
            private readonly MessageSerializer serializer;
            private readonly Guid guid = Guid.NewGuid();

            public HttpClientRuntime(IServiceProvider serviceProvider)
            {
                this.bindingOption = serviceProvider.GetRequiredService<BasicHttpBindingOptions>();
                this.serializer = serviceProvider.GetRequiredService<MessageSerializer>();
            }

            public override void SendMessage(Message message)
            {
                var bytes = serializer.Serialize(message);
                var webRequest = (HttpWebRequest)WebRequest.Create(bindingOption.Url);
                webRequest.Method = WebRequestMethods.Http.Post;
                webRequest.ContentType = "application/octet-stream";
                webRequest.ContentLength = bytes.Length;
                if (webRequest.SupportsCookieContainer)
                {
                    webRequest.CookieContainer.Add(new Cookie(SessionCookieKey, guid.ToString()));
                }
                using (var inputStream = webRequest.GetRequestStream())
                {
                    inputStream.Write(bytes, 0, bytes.Length);
                }
                using (var webResponse = webRequest.GetResponse())
                {
                    if (message.Direction.HasFlag(Message.Directions.OneWay))
                    {
                        return;
                    }

                    bytes = new byte[webResponse.ContentLength];
                    using (var outputStream = webResponse.GetResponseStream())
                    {
                        outputStream.Read(bytes, 0, bytes.Length);
                    }
                }

                message = serializer.Deserialize(bytes);
                Dispatch(message);
            }
        }
    }
}
