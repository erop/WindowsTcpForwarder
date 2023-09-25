namespace WindowsTcpForwarder.Configuration;

public class SourceSettings
{
    public const string Section = "Source";
    public string LocalIp { get; set; } = "127.0.0.1";
    public int Port { get; set; }
}