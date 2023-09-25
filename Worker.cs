using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using WindowsTcpForwarder.Configuration;

namespace WindowsTcpForwarder;

public class Worker : BackgroundService
{
    private readonly List<NetworkStream> _destinations = new();
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
            _source.Start();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to instantiate TCP listener on interface {LocalIp} and port {Port}",
                sourceSettings.LocalIp, sourceSettings.Port);
            ShutdownApplication(1);
        }
    }

    private void ShutdownApplication(int code)
    {
        _source?.Stop();
        foreach (var stream in _destinations) stream.Dispose();
        Environment.Exit(code);
    }

    private void InitializeDestinations(DestinationsSettings destinationsSettings)
    {
        _destinations.Clear();
        foreach (var destinationSetting in destinationsSettings.Destinations)
            try
            {
                var tcpClient = new TcpClient(destinationSetting.Host, destinationSetting.Port);
                _destinations.Add(tcpClient.GetStream());
            }
            catch (Exception e) when (e is ArgumentNullException || e is ArgumentOutOfRangeException ||
                                      e is SocketException)
            {
                _logger.LogError("Initializing destination stream error: {Message}", e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError("Unexpected error on TCP client initialization: IP {IP}, Port {Port} ",
                    destinationSetting.Host, destinationSetting.Port);
            }

        if (_destinations.Count == 0)
        {
            _logger.LogCritical("No TCP clients were initialized");
            ShutdownApplication(1);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var buffer = new byte[256];
            while (!stoppingToken.IsCancellationRequested)
                if (_source is not null)
                {
                    NetworkStream? sourceStream = null;
                    var client = await _source.AcceptTcpClientAsync(stoppingToken);
                    try
                    {
                        sourceStream = client.GetStream();
                    }
                    catch (Exception e) when (e is InvalidOperationException || e is ObjectDisposedException)
                    {
                        _logger.LogError(e, "Unable to acquire source network stream");
                        ShutdownApplication(1);
                    }

                    int i;

                    while ((i = await sourceStream.ReadAsync(buffer, 0, buffer.Length, stoppingToken)) != 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, i);
                        _logger.LogInformation("Received: {Message}", message);
                        foreach (var destination in _destinations)
                            try
                            {
                                destination.Write(buffer);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Unable to write to destination network stream");
                            }
                    }
                }
        }
        catch (TaskCanceledException e)
        {
            _logger.LogInformation("{Message}", e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError( "Application error: {Message}", e.Message);
            ShutdownApplication(1);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        ShutdownApplication(0);
        return base.StopAsync(cancellationToken);
    }
}