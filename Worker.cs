using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using WindowsTcpForwarder.Configuration;

namespace WindowsTcpForwarder;

public class Worker : BackgroundService
{
    private readonly DestinationsSettings _destinationsSettings;
    private readonly ILogger<Worker> _logger;
    private readonly SourceSettings _sourceSettings;
    private List<StreamWriter> _destinations = new();
    private TcpListener? _listener;

    public Worker(ILogger<Worker> logger, IOptions<SourceSettings> sourceSettings,
        IOptions<DestinationsSettings> destinationsSettings)
    {
        _logger = logger;
        _sourceSettings = sourceSettings.Value;
        _destinationsSettings = destinationsSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = InitializeListener(_sourceSettings);
        _destinations = InitializeDestination(_destinationsSettings);
    }

    private List<StreamWriter> InitializeDestination(DestinationsSettings settings)
    {
        foreach (var hostPort in settings.Destinations)
        {
            new TcpClient(hostPort.Host, hostPort.Port);
        }
    }

    private TcpListener InitializeListener(SourceSettings settings)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Parse(settings.LocalIp), settings.Port);
            listener.Start();
            return listener;
        }
        catch (Exception e)
        {
            _logger.LogError("Unable to initialize TCP listener: {Message}", e.Message);
            ShutdownApplication(1);
        }
    }

    private void ShutdownApplication(int code)
    {
        _listener?.Stop();
        foreach (var destination in _destinations) destination.Close();
        Environment.Exit(code);
    }
}