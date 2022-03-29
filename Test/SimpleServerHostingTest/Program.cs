using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Framework.Log;
using SimpleFrameworkTest;
using SimpleFrameworkTest.Services;
using ZyGames.Framework.Services;
using ZyGames.Framework.Services.Dashboard;
using ZyGames.Framework.Services.Options;

namespace SimpleServerHostingTest
{
    class Program
    {
        private static ServiceHost serviceHost;

        static void Main(string[] args)
        {
            Logger.AddFactory<ConsoleLoggerFactory>();
            var builder = new ServiceHostBuilder();
            var address = Dns.GetHostAddresses(Dns.GetHostName()).First(p => p.AddressFamily == AddressFamily.InterNetwork);
            builder.AddSystemTarget<IClusterMembershipService, ClusterMembershipService, ClusterMembershipServiceOptions>(p =>
            {
                p.InsideAddress = new Address(address.ToString(), 64000);
                p.Backlog = 10;
                p.MaxConnections = 100;
            });
            builder.AddSystemTarget<IGatewayMembershipService, GatewayMembershipService, GatewayMembershipServiceOptions>(p =>
            {
                p.InsideAddress = new Address(address.ToString(), 64001);
                p.Backlog = 10;
                p.MaxConnections = 100;
                p.Cluster = new Address(address.ToString(), 64000);
                p.RequestTimeout = TimeSpan.FromSeconds(3);
                //p.Cluster = new SlioAddress("172.17.70.14", 65000);
            });
            //builder.AddComponent<TaskScheduler, ServiceTaskScheduler>();
            builder.AddDashboard();
            builder.UseDashboard();

            builder.AddService<ISayService, SayService>(metadata: new ServiceMetadata() { ID = 45 });
            builder.AddService<IHelloService, HelloService>();
            builder.AddService<IWorldService, WorldService>();
            //ThreadScheduler.Current.BlockingWorkerThread = ThreadScheduler.Current.BlockingWorkerThread / 2;
            serviceHost = builder.Build();
            serviceHost.Start();
            Console.WriteLine("started...");
            Console.ReadLine();
        }
    }
}
