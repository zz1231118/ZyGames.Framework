# ZyGames.Framework

## 一：功能介绍

### 1.示例：
```C#
public interface ISayService : IService
{
    string Say(string name);
}

public interface IHelloService : IService
{
    string SayHello(string name);
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
        return string.Format("hello {0}", name);
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
        serviceHost = builder.Build();
        serviceHost.Lifecycle.WithStarted(nameof(Program), (token) =>
        {
            var serviceFactory = serviceHost.ServiceFactory;
            var helloService = serviceFactory.GetSingleService<ISayService>();
            var metadata = helloService.Metadata;
            var result = helloService.Say("cxx");
			//result: say hello cxx
        });

        serviceHost.Start();
        Console.WriteLine("started...");

        Console.ReadLine();
    }
}
```
