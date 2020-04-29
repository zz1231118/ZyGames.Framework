# ZyGames.Framework

```C#
public interface ISayService : IService
{
    string Say(string name);
}

public interface IHelloService : IService
{
    string SayHello(string name);
}

public interface IWorldService : IService
{
    string SayWorld(string name);
}

public class SayService : Service, ISayService
{
    public string Say(string name)
    {
        var helloService = ServiceFactory.GetSingleService<IHelloService>();
        return string.Format("say {0}", helloService.SayHello(name));
    }
}

public class HelloService : Service, IHelloService
{
    public string SayHello(string name)
    {
        var worldService = ServiceFactory.GetSingleService<IWorldService>();
        return string.Format("hello {0}", worldService.SayWorld(name));
    }
}

public class WorldService : Service, IWorldService
{
    public string SayWorld(string name)
    {
        return string.Format("world {0}", name);
    }
}

class Program
{
    private static ServiceHost serviceHost;

    static void Main(string[] args)
    {
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddProvider<ConsoleLoggerProvider>();
        Logger.LoggerFactory = loggerFactory;

        var builder = new ServiceHostBuilder();
        builder.ConfigureOptions<ClusterMembershipServiceOptions>(p =>
        {
            p.InsideAddress = new SlioAddress("0.0.0.0", 64000);
            p.Backlog = 10;
            p.MaxConnections = 100;
        });
        builder.ConfigureOptions<GatewayMembershipServiceOptions>(p =>
        {
            p.InsideAddress = new SlioAddress("0.0.0.0", 64001);
            p.Backlog = 10;
            p.MaxConnections = 100;
            p.Cluster = new SlioAddress("0.0.0.0", 64000);
        });
        builder.AddSystemTarget<IClusterMembershipService, ClusterMembershipService>();
        builder.AddSystemTarget<IGatewayMembershipService, GatewayMembershipService>();

        builder.AddService<ISayService, SayService>(metadata: new ServiceMetadata() { ID = 45 });
        builder.AddService<IHelloService, HelloService>();
        builder.AddService<IWorldService, WorldService>();
        serviceHost = builder.Build();
        serviceHost.Lifecycle.WithStarted(nameof(Program), (token) =>
        {
            var serviceFactory = serviceHost.ServiceFactory;
            var helloService = serviceFactory.GetSingleService<ISayService>();
            var metadata = helloService.Metadata;
            var result = helloService.Say("cxx");
        });

        serviceHost.Start();
        Console.WriteLine("started...");

        Console.ReadLine();
    }
}
```