using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpTester.Models;

public enum TransportType
{
    Stdio,
    Sse
}

public class ServerConfig
{
    [JsonPropertyName("transport")]
    public string Transport { get; set; } = "stdio";

    // Para Stdio
    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("args")]
    public string[] Args { get; set; } = [];

    [JsonPropertyName("env")]
    public Dictionary<string, string>? EnvVars { get; set; }

    // Para SSE
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    public TransportType GetTransportType() =>
        Transport.Equals("sse", StringComparison.OrdinalIgnoreCase)
            ? TransportType.Sse
            : TransportType.Stdio;
}

public class McpConfig
{
    [JsonPropertyName("servers")]
    public Dictionary<string, ServerConfig> Servers { get; set; } = new();
}
