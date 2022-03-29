using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Framework.Log;
using SimpleFrameworkTest;
using ZyGames.Framework.Services;
using ZyGames.Framework.Services.Options;
using ZyGames.Framework.Services.Dashboard;

namespace SimpleClientHostingTest
{
    class Program
    {
        private static ILogger<Program> logger;
        private static ServiceHost serviceHost;

        static void Main(string[] args)
        {
            Logger.AddFactory<ConsoleLoggerFactory>();
            logger = Logger.GetLogger<Program>();

            var builder = new ServiceHostBuilder();
            var address = Dns.GetHostAddresses(Dns.GetHostName()).First(p => p.AddressFamily == AddressFamily.InterNetwork);
            builder.AddSystemTarget<IGatewayMembershipService, GatewayMembershipService, GatewayMembershipServiceOptions>(p =>
            {
                p.InsideAddress = new Address(address.ToString(), 64002);
                p.Backlog = 10;
                p.MaxConnections = 100;
                p.Cluster = new Address(address.ToString(), 64000);
                p.RequestTimeout = TimeSpan.FromSeconds(3);
                //p.Cluster = new SlioAddress("172.17.70.14", 65000);
            });
            builder.UseDashboard();

            //builder.AddComponent<TaskScheduler, ServiceTaskScheduler>();

            serviceHost = builder.Build();
            serviceHost.Lifecycle.WithStarted(nameof(Program), (token) =>
            {
                Console.WriteLine("started...");
                var serviceFactory = serviceHost.ServiceFactory;
                var completed = 0;
                var count = 30 * 1000;
                var threads = new List<Thread>();
                var countdown = new CountdownEvent(count);
                var manual = new ManualResetEventSlim();
                for (int i = 0; i < 26; i++)
                {
                    var thread = new Thread(() => 
                    {
                        manual.Wait();
                        var service = serviceFactory.GetService<IWorldService>();
                        while (true)
                        {
                            var value = Interlocked.Increment(ref completed);
                            if (value > count) break;

                            service.World("bb");
                            countdown.Signal();
                        }
                    });
                    thread.Start();
                    threads.Add(thread);
                }
                var stopwatch = Stopwatch.StartNew();
                manual.Set();
                countdown.Wait();
                stopwatch.Stop();
                var elapsed = stopwatch.Elapsed;
                var qps = count / elapsed.TotalSeconds;
                logger.Info("elapsed: {0}", elapsed);
                logger.Info("qps: {0}", qps);

                System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(p => 
                {
                    var service = serviceFactory.GetService<IDashboardService>();
                    var tracing = service.GetClusterTracing();
                });
            });

            serviceHost.Start();
            Console.ReadLine();
        }
    }
}
