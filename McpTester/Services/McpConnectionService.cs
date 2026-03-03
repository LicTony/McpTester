using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpTester.Models;
using McpDotNet.Client;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace McpTester.Services;

public class McpConnectionService : IDisposable
{
    private readonly Dictionary<string, IMcpClient> _clients = new();

    public IReadOnlyDictionary<string, IMcpClient> Clients => _clients;

    public async Task<IMcpClient> ConnectStdioAsync(string name, ServerConfig config)
    {
        var serverConfig = new McpServerConfig
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            TransportType = "stdio",
            Location = config.Command,
            Arguments = config.Args ?? Array.Empty<string>()
        };

        var clientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "McpTester", Version = "1.0.0" },
            InitializationTimeout = TimeSpan.FromSeconds(60)
        };

        var client = await McpClientFactory.CreateAsync(serverConfig, clientOptions);
        _clients[name] = client;
        return client;
    }

    public async Task<IMcpClient> ConnectSseAsync(string name, ServerConfig config)
    {
        var serverConfig = new McpServerConfig
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            TransportType = "sse",
            Location = config.Url
        };

        var clientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "McpTester", Version = "1.0.0" },
            InitializationTimeout = TimeSpan.FromSeconds(60)
        };

        var client = await McpClientFactory.CreateAsync(serverConfig, clientOptions);
        _clients[name] = client;
        return client;
    }

    public async Task<IList<Tool>> ListToolsAsync(string serverName)
    {
        if (!_clients.TryGetValue(serverName, out var client))
            throw new InvalidOperationException($"Server '{serverName}' no está conectado.");

        var tools = new List<Tool>();
        await foreach (var tool in client.ListToolsAsync())
        {
            tools.Add(tool);
        }
        return tools;
    }

    public async Task<CallToolResponse> CallToolAsync(
        string serverName,
        string toolName,
        Dictionary<string, object?> args)
    {
        if (!_clients.TryGetValue(serverName, out var client))
            throw new InvalidOperationException($"Server '{serverName}' no está conectado.");

        // Convert Dictionary<string, object?> to Dictionary<string, object> to match API nulability
        var nonNullableArgs = args.ToDictionary(k => k.Key, v => v.Value ?? new object());
        return await client.CallToolAsync(toolName, nonNullableArgs);
    }

    public void DisconnectAll()
    {
        // IMcpClient might not be IDisposable, but we can clear the dictionary.
        // If the library provides a way to close connections, it should be called here.
        _clients.Clear();
    }

    public void Dispose() => DisconnectAll();
}
