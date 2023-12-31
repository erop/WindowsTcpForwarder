using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using WindowsTcpForwarder.Configuration;

namespace WindowsTcpForwarder;

public class Worker : BackgroundService
{
    private readonly List<StreamWriter> _destinations = new();
    private readonly DestinationsSettings _destinationsSettings;
    private readonly ILogger<Worker> _logger;
    private readonly SourceSettings _sourceSettings;
    private TcpListener? _source;

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

        try
        {
            while (!stoppingToken.IsCancellationRequested)
                if (_source is not null)
                {
                    var client = await _source.AcceptTcpClientAsync(stoppingToken);
                    var stream = client.GetStream();
                    var reader = new StreamReader(stream);

                    try
                    {
                        while (await reader.ReadLineAsync(stoppingToken) is { } message)
                        {
                            _logger.LogInformation("[{Time}] Message: {Message}", DateTimeOffset.Now.ToString("u"),
                                message);
                            foreach (var writer in _destinations)
                                try
                                {
                                    await writer.WriteLineAsync(message);
                                    await writer.FlushAsync();
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError("Unable to send message");
                                }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Error processing message", e.Message);
                    }
                }
        }
        catch (Exception e)
        {
            _logger.LogError("Unable to initialize stream reader: {Message}", e.Message);
            ShutdownFailedApplication();
        }

        ShutdownApplication(0);
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
                _logger.LogWarning("Unable to initialize TcpClient for host:port {Host}:{Port}; Message: {Message}",
                    hostPort.Host,
                    hostPort.Port, e.Message);
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
            _source = new TcpListener(IPAddress.Parse(settings.LocalIp), settings.Port);
            _source.Start();
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
        foreach (var destination in _destinations) destination.Close();
        _source?.Stop();
        Environment.Exit(code);
    }
}