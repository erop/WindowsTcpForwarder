using WindowsTcpForwarder;
using WindowsTcpForwarder.Configuration;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<SourceSettings>()
            .Bind(context.Configuration.GetSection(SourceSettings.Section));
        services.AddOptions<DestinationsSettings>()
            .Bind(context.Configuration.GetSection(DestinationsSettings.Section));

        services.AddWindowsService(options => { options.ServiceName = "TcpForwarder"; });

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();