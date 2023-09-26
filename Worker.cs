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
        InitializeSource(_sourceSettings);
        InitializeDestinations(_destinationsSettings);
    }

    private void InitializeDestinations(DestinationsSettings settings)
    {
        _destinations.Clear();
        foreach (var hostPort in settings.Destinations)
            try
            {
                var client = new TcpClient(hostPort.Host, hostPort.Port);
                var stream = client.GetStream();
                var writer = new StreamWriter(stream);
                _destinations.Add(writer);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Unable to initialize TcpClient for host:port {Host}:{Port}", hostPort.Host, hostPort.Port);
            }

        if (_destinations.Count == 0)
        {
            _logger.LogError("No destination were initialized");
            ShutdownFailedApplication();
        }
    }

    private void InitializeSource(SourceSettings settings)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Parse(settings.LocalIp), settings.Port);
            _listener.Start();
        }
        catch (Exception e)
        {
            _logger.LogError("Unable to initialize TCP listener: {Message}", e.Message);
            ShutdownFailedApplication();
        }
    }

    private void ShutdownFailedApplication()
    {
        ShutdownApplication(1);
    }

    private void ShutdownApplication(int code)
    {
        _listener?.Stop();
        foreach (var destination in _destinations) destination.Close();
        Environment.Exit(code);
    }
}