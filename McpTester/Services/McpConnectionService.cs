using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpTester.Models;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace McpTester.Services;

public class McpConnectionService : IDisposable
{
    private readonly Dictionary<string, McpClient> _clients = new();

    public IReadOnlyDictionary<string, McpClient> Clients => _clients;

    public async Task<McpClient> ConnectStdioAsync(string name, ServerConfig config)
    {
        var transportOptions = new StdioClientTransportOptions
        {
            Command = config.Command,
            Arguments = config.Args ?? Array.Empty<string>(),
            EnvironmentVariables = config.EnvVars?.ToDictionary(k => k.Key, k => (string?)k.Value)
        };
        var transport = new StdioClientTransport(transportOptions);

        var clientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "McpTester", Version = "1.0.0" }
        };

        var client = await McpClient.CreateAsync(transport, clientOptions);
        _clients[name] = client;
        return client;
    }

    public async Task<McpClient> ConnectSseAsync(string name, ServerConfig config)
    {
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(config.Url ?? throw new ArgumentNullException(nameof(config.Url))),
            TransportMode = config.GetTransportType() == TransportType.Sse ? HttpTransportMode.Sse : HttpTransportMode.StreamableHttp
        };
        var transport = new HttpClientTransport(transportOptions);
        
        var clientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "McpTester", Version = "1.0.0" }
        };

        var client = await McpClient.CreateAsync(transport, clientOptions);
        _clients[name] = client;
        return client;
    }

    public async Task<IList<Tool>> ListToolsAsync(string serverName)
    {
        if (!_clients.TryGetValue(serverName, out var client))
            throw new InvalidOperationException($"Server '{serverName}' no está conectado.");

        var response = await client.ListToolsAsync();
        return response.Select(t => t.ProtocolTool).ToList();
    }

    public async Task<CallToolResult> CallToolAsync(
        string serverName,
        string toolName,
        Dictionary<string, object?> args)
    {
        if (!_clients.TryGetValue(serverName, out var client))
            throw new InvalidOperationException($"Server '{serverName}' no está conectado.");

        // CallToolAsync might take a Dictionary<string, object?> or a specific options class
        var callArgs = args.ToDictionary(k => k.Key, v => System.Text.Json.JsonSerializer.SerializeToElement(v.Value ?? new object()));
        var param = new CallToolRequestParams { Name = toolName, Arguments = callArgs };
        return await client.CallToolAsync(param);
    }

    public void DisconnectAll()
    {
        foreach (var client in _clients.Values)
        {
            if (client is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
        }
        _clients.Clear();
    }

    public void Dispose() => DisconnectAll();
}
