using System;
using SwordWorld.GameServers.Services;
using ZyGames.Framework.Services;
using ZyGames.Framework.Services.Options;

namespace SwordWorld.GameServers
{
    class Program
    {
        private static ServiceHost serviceHost;

        static void Main(string[] args)
        {
            var builder = new ServiceHostBuilder();
            builder.ConfigureOptions<ClusterMembershipServiceOptions>();
            builder.ConfigureOptions<GatewayMembershipServiceOptions>();
            builder.AddSystemTarget<IClusterMembershipService, ClusterMembershipService>();
            builder.AddSystemTarget<IGatewayMembershipService, GatewayMembershipService>();
            builder.AddService<IDataService, DataService>();

            serviceHost = builder.Build();
            serviceHost.Start();
            Console.WriteLine("started...");
            Console.ReadLine();
            serviceHost.Stop();
        }
    }
}
