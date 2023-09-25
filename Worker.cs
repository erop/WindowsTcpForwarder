using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using WindowsTcpForwarder.Configuration;

namespace WindowsTcpForwarder;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private TcpListener? _source;

    public Worker(ILogger<Worker> logger, IOptions<SourceSettings> sourceSettings,
        IOptions<DestinationsSettings> destinationsSettings)
    {
        _logger = logger;
        InitializeSource(sourceSettings.Value);
        InitializeDestinations(destinationsSettings.Value);
    }

    private void InitializeSource(SourceSettings sourceSettings)
    {
        try
        {
            _source = new TcpListener(IPAddress.Parse(sourceSettings.LocalIp), sourceSettings.Port);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to instantiate TCP listener on interface {LocalIp} and port {Port}",
                sourceSettings.LocalIp, sourceSettings.Port);
            Environment.Exit(1);
        }
    }

    private void InitializeDestinations(DestinationsSettings destinationsSettings)
    {
        foreach (var destinationSetting in destinationsSettings)
        {
            
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            Environment.Exit(1);
        }
    }
}