using System.ComponentModel.DataAnnotations;

namespace WindowsTcpForwarder.Configuration;

public class SourceSettings
{
    public const string Section = "Source";

    [Required] public string LocalIp { get; set; } = "127.0.0.1";

    [Required] public int Port { get; set; }
}