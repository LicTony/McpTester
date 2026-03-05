using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpTester.Models;

public enum TransportType
{
    Stdio,
    Sse,
    StreamableHttp
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

    // Para SSE / StreamableHttp
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    public TransportType GetTransportType()
    {
        if (Transport.Equals("sse", StringComparison.OrdinalIgnoreCase))
            return TransportType.Sse;
        if (Transport.Equals("streamableHttp", StringComparison.OrdinalIgnoreCase))
            return TransportType.StreamableHttp;
        return TransportType.Stdio;
    }
}

public class McpConfig
{
    [JsonPropertyName("servers")]
    public Dictionary<string, ServerConfig> Servers { get; set; } = new();
}
