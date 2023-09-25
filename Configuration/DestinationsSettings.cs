namespace WindowsTcpForwarder.Configuration;

public class DestinationsSettings
{
    public const string Section = "Destinations";
    public List<HostPort> Destinations { get; set; } = new();
}

public class HostPort
{
    public required string Host { get; set; }
    public int Port { get; set; }
}