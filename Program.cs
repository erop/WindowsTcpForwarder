using WindowsTcpForwarder;
using WindowsTcpForwarder.Configuration;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<SourceSettings>()
            .BindConfiguration(SourceSettings.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<DestinationsSettings>()
            .BindConfiguration(DestinationsSettings.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddWindowsService(options => { options.ServiceName = "TcpForwarder"; });

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();