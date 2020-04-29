using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ZyGames.Framework.Remote;
using ZyGames.Framework.Remote.Networking;

namespace Remote.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var pipeName = Guid.NewGuid().ToString("N");
            var addresses = Dns.GetHostAddresses(Dns.GetHostName());
            var address = addresses.First(p => p.AddressFamily == AddressFamily.InterNetwork);
            var endpoint = new IPEndPoint(address, 65530);
            var serviceHostBuilder = new ServiceHostBuilder()
                //.ConfigureSingleOption<ServiceOption>(option =>
                //{
                //    option.Credentials = new ServerCredentials(pipeName);
                //})
                .AddSingleService<LoginService>()
                .AddSingleService<CryptoTransformService>()
                .ConfigureSingleBinding<BasicPipeBinding>()
                .ConfigureSingleOption<BasicPipeBindingOptions>(option =>
                {
                    option.PipeName = pipeName;
                });
            //.ConfigureSingleBinding<BasicTcpBinding>()
            //.ConfigureSingleOption<BasicTcpBindingOption>(option =>
            //{
            //    option.EndPoint = endpoint;
            //    option.Backlog = 4;
            //    option.MaxAcceptOps = 10;
            //    option.MaxConnections = 100;
            //});
            //.ConfigureSingleBinding<BasicHttpBinding>()
            //.ConfigureSingleOption<BasicHttpBindingOption>(option =>
            //{
            //    option.Url = "http://localhost/";
            //});
            var serviceHost = serviceHostBuilder.Build();
            serviceHost.Start();

            Console.WriteLine("started...");

            var clientBuilder = new ClientBuilder()
                //.ConfigureSingleOption<ClientOption>(option =>
                //{
                //    option.Credentials = new ClientCredential("sss", pipeName);
                //})
                .ConfigureSingleBinding<BasicPipeBinding>()
                .ConfigureSingleOption<BasicPipeBindingOptions>(option =>
                {
                    option.ServiceName = ".";
                    option.PipeName = pipeName;
                });
            //.ConfigureSingleBinding<BasicTcpBinding>()
            //.ConfigureSingleOption<BasicTcpBindingOption>(option =>
            //{
            //    option.EndPoint = endpoint;
            //});
            //.ConfigureSingleBinding<BasicHttpBinding>()
            //.ConfigureSingleOption<BasicHttpBindingOption>(option =>
            //{
            //    option.Url = "http://localhost/";
            //});
            var client = clientBuilder.Build();
            var loginService = client.GetService<ILoginService>();
            var cryptoTransformService = client.GetService<ICryptoTransformService>();
            var result = loginService.Login("zzz", "xxx");
            var loginToken = loginService.GetToken("zzz");

            var bytes = cryptoTransformService.Encrypt(loginToken, "hello world!");
            var text = cryptoTransformService.Decrypt(loginToken, bytes);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var times = 10000;
            for (int i = 0; i < times; i++)
            {
                loginService.Hello();
            }

            stopwatch.Stop();
            var value = times / stopwatch.Elapsed.TotalSeconds;
            var duration = stopwatch.Elapsed;
            Console.ReadLine();
        }
    }

    [ServiceContract]
    public interface ILoginService : IService
    {
        [OperationContract]
        bool Login(string account, string password);

        [OperationContract]
        LoginToken GetToken(string account);

        [OperationContract]
        void Hello();

        [OperationContract(Options = InvokeMethodOptions.OneWay)]
        void HelloAsync();
    }

    [ServiceContract]
    public interface ICryptoTransformService : IService
    {
        byte[] Encrypt(LoginToken token, string text);

        string Decrypt(LoginToken token, byte[] bytes);
    }

    [Serializable]
    public class LoginToken
    {
        public LoginToken(string account, DateTime timestamp, string token)
        {
            Account = account;
            Timestamp = timestamp;
            Token = token;
        }

        public string Account { get; }

        public DateTime Timestamp { get; }

        public string Token { get; }
    }

    public class LoginService : ILoginService
    {
        private readonly Dictionary<string, LoginToken> loginTokens = new Dictionary<string, LoginToken>();

        public bool Login(string account, string password)
        {
            if (account == "zzz" && password == "xxx")
            {
                var timestamp = DateTime.Now;
                var token = Guid.NewGuid().ToString("N");
                loginTokens[account] = new LoginToken(account, timestamp, token);
                return true;
            }

            return false;
        }

        public LoginToken GetToken(string account)
        {
            loginTokens.TryGetValue(account, out LoginToken token);
            return token;
        }

        public void Hello()
        {
            //System.Threading.Thread.Sleep(3 * 1000);
        }

        public void HelloAsync()
        { }
    }

    public class CryptoTransformService : ICryptoTransformService
    {
        private bool IsAvailable(LoginToken token)
        {
            return true;
        }

        public byte[] Encrypt(LoginToken token, string text)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (!IsAvailable(token))
                throw new InvalidOperationException("invalid token.");

            return System.Text.Encoding.UTF8.GetBytes(text);
        }

        public string Decrypt(LoginToken token, byte[] bytes)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (!IsAvailable(token))
                throw new InvalidOperationException("invalid token.");

            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
