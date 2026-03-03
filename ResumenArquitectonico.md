

# App WPF para Testing de MCPs — Resumen Arquitectónico

## Stack y Paquetes NuGet

```
Target: net10.0-windows
```

| Paquete | Propósito |
|---|---|
| `ModelContextProtocol` | SDK oficial C# MCP (Microsoft) |
| `CommunityToolkit.Mvvm` | MVVM source generators |
| `AvalonEdit` | Editor JSON para params/resultados |
| `System.Text.Json` | Serialización (ya incluido) |

---

## Arquitectura General

```
┌─────────────────────────────────────────────────┐
│                    WPF App                       │
│                                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────┐│
│  │  Server      │  │  Tool       │  │ Result   ││
│  │  Config      │  │  Explorer   │  │ Viewer   ││
│  │  Panel       │  │  + Params   │  │          ││
│  └──────┬───────┘  └──────┬──────┘  └────┬─────┘│
│         │                 │              │       │
│  ┌──────▼─────────────────▼──────────────▼─────┐│
│  │            McpConnectionService              ││
│  │  (gestiona N conexiones a MCP servers)       ││
│  └──────────────────┬──────────────────────────┘│
│                     │                            │
│  ┌──────────────────▼──────────────────────────┐│
│  │         ModelContextProtocol SDK             ││
│  │    ┌──────────┐      ┌───────────┐          ││
│  │    │  stdio   │      │ SSE/HTTP  │          ││
│  │    │ transport│      │ transport │          ││
│  │    └──────────┘      └───────────┘          ││
│  └─────────────────────────────────────────────┘│
└─────────────────────────────────────────────────┘
         │                        │
    ┌────▼─────┐            ┌─────▼──────┐
    │ MCP      │            │ MCP        │
    │ Server A │            │ Server B   │
    │ (stdio)  │            │ (SSE)      │
    └──────────┘            └────────────┘
```

---

## Componentes Clave (5 piezas)

### 1. `McpConnectionService` — El núcleo

```csharp
public class McpConnectionService
{
    // Una conexión por servidor configurado
    private readonly Dictionary<string, IMcpClient> _clients = new();

    // Conectar a un server stdio
    public async Task<IMcpClient> ConnectStdioAsync(ServerConfig config)
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = config.Command,        // "npx", "uvx", "node", etc.
            Arguments = config.Args,          // ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"]
            EnvironmentVariables = config.EnvVars
        });

        var client = await McpClientFactory.CreateAsync(transport);
        _clients[config.Name] = client;
        return client;
    }

    // Conectar a un server SSE
    public async Task<IMcpClient> ConnectSseAsync(ServerConfig config)
    {
        var transport = new SseClientTransport(new SseClientTransportOptions
        {
            Endpoint = new Uri(config.Url)    // "http://localhost:3001/sse"
        });

        var client = await McpClientFactory.CreateAsync(transport);
        _clients[config.Name] = client;
        return client;
    }

    // Listar tools — ESTO ES LO PRINCIPAL
    public async Task<IList<McpClientTool>> ListToolsAsync(string serverName)
    {
        return await _clients[serverName].ListToolsAsync();
    }

    // Llamar a una tool con parámetros arbitrarios
    public async Task<CallToolResponse> CallToolAsync(
        string serverName, string toolName, Dictionary<string, object?> args)
    {
        return await _clients[serverName].CallToolAsync(toolName, args);
    }
}
```

### 2. `ServerConfig` — Modelo de configuración

```csharp
public class ServerConfig
{
    public string Name { get; set; }
    public TransportType Transport { get; set; }  // Stdio | Sse

    // Para Stdio
    public string Command { get; set; }           // "npx", "python", "dotnet"
    public string[] Args { get; set; }
    public Dictionary<string, string>? EnvVars { get; set; }

    // Para SSE
    public string? Url { get; set; }
}

// Persistir en un JSON estilo Claude Desktop:
// mcp-servers.json
```

El JSON de config sería compatible con el formato estándar:

```json
{
  "servers": {
    "filesystem": {
      "transport": "stdio",
      "command": "npx",
      "args": ["-y", "@anthropic/mcp-server-filesystem", "/tmp"],
      "env": { "DEBUG": "true" }
    },
    "mi-api": {
      "transport": "sse",
      "url": "http://localhost:3001/sse"
    }
  }
}
```

### 3. Layout WPF — 3 paneles

```
┌──────────────────────────────────────────────────────────┐
│ [Toolbar: Abrir Config | Conectar | Desconectar | Logs] │
├────────────┬──────────────────────┬──────────────────────┤
│            │                      │                      │
│  SERVERS   │   TOOL DETAIL        │   RESULTADO          │
│  & TOOLS   │                      │                      │
│            │  Tool: read_file     │  ┌────────────────┐  │
│  ▼ filesystem │                   │  │ {              │  │
│    ☐ read_file │  Descripción:   │  │   "content": [ │  │
│    ☐ write_file│  "Lee un archivo"│  │     {          │  │
│    ☐ list_dir  │                  │  │       "type":  │  │
│            │  Parámetros:         │  │       "text",  │  │
│  ▼ mi-api  │  ┌────────────────┐ │  │       "text":  │  │
│    ☐ query │  │ path: [_____]  │ │  │       "..."    │  │
│    ☐ insert│  │ encoding: [___]│ │  │     }          │  │
│            │  └────────────────┘ │  │   ]            │  │
│            │                      │  │ }              │  │
│            │  [▶ EJECUTAR]        │  └────────────────┘  │
│            │                      │                      │
│            │  -- ó JSON raw: --   │  Status: ✅ Success  │
│            │  ┌────────────────┐ │  Time: 45ms          │
│            │  │ { "path": "/"} │ │                      │
│            │  └────────────────┘ │                      │
├────────────┴──────────────────────┴──────────────────────┤
│ [Log: 10:23:01 → tools/list OK | 10:23:05 → tools/call] │
└──────────────────────────────────────────────────────────┘
```

### 4. Generador Dinámico de Formularios (lo más complejo)

Cada tool MCP expone su `inputSchema` como JSON Schema. Necesitas generar UI dinámicamente:

```csharp
public class ToolParameterFormBuilder
{
    // Recibe el JsonSchema de la tool y genera controles WPF
    public UIElement BuildForm(JsonElement inputSchema)
    {
        var panel = new StackPanel();
        var properties = inputSchema.GetProperty("properties");
        var required = inputSchema.TryGetProperty("required", out var req)
            ? req.EnumerateArray().Select(x => x.GetString()).ToHashSet()
            : new HashSet<string?>();

        foreach (var prop in properties.EnumerateObject())
        {
            var type = prop.Value.GetProperty("type").GetString();
            var description = prop.Value.TryGetProperty("description", out var desc)
                ? desc.GetString() : "";

            var label = new TextBlock
            {
                Text = $"{prop.Name}{(required.Contains(prop.Name) ? " *" : "")}",
                ToolTip = description
            };
            panel.Children.Add(label);

            UIElement control = type switch
            {
                "string" => new TextBox { Tag = prop.Name },
                "number" or "integer" => new TextBox { Tag = prop.Name },
                "boolean" => new CheckBox { Tag = prop.Name },
                "array" => new TextBox { Tag = prop.Name, AcceptsReturn = true }, // JSON
                "object" => new TextBox { Tag = prop.Name, AcceptsReturn = true }, // JSON
                _ => new TextBox { Tag = prop.Name }
            };

            // Si tiene enum, usar ComboBox
            if (prop.Value.TryGetProperty("enum", out var enumValues))
            {
                var combo = new ComboBox { Tag = prop.Name };
                foreach (var v in enumValues.EnumerateArray())
                    combo.Items.Add(v.GetString());
                control = combo;
            }

            panel.Children.Add(control);
        }
        return panel;
    }

    // Recolectar valores del form → Dictionary<string, object?>
    public Dictionary<string, object?> CollectValues(StackPanel panel) { ... }
}
```

**Alternativa más simple**: solo un editor JSON raw donde el usuario escribe los params. Más rápido de implementar, igual de funcional para testing.

### 5. ViewModel Principal

```csharp
[ObservableObject]
public partial class MainViewModel
{
    private readonly McpConnectionService _mcp = new();

    [ObservableProperty] ObservableCollection<ServerViewModel> servers = new();
    [ObservableProperty] McpClientTool? selectedTool;
    [ObservableProperty] string parameterJson = "{}";
    [ObservableProperty] string resultJson = "";
    [ObservableProperty] string statusText = "";
    [ObservableProperty] bool isExecuting;

    [RelayCommand]
    async Task LoadConfig()
    {
        var config = JsonSerializer.Deserialize<McpConfig>(
            await File.ReadAllTextAsync("mcp-servers.json"));

        foreach (var (name, server) in config.Servers)
        {
            var client = server.Transport switch
            {
                "stdio" => await _mcp.ConnectStdioAsync(server),
                "sse"   => await _mcp.ConnectSseAsync(server),
            };

            var tools = await _mcp.ListToolsAsync(name);
            Servers.Add(new ServerViewModel(name, tools));
        }
    }

    [RelayCommand]
    async Task ExecuteTool()
    {
        if (SelectedTool is null) return;
        IsExecuting = true;

        var args = JsonSerializer.Deserialize<Dictionary<string, object?>>(ParameterJson);
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await _mcp.CallToolAsync(serverName, SelectedTool.Name, args);
            sw.Stop();

            ResultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            StatusText = $"✅ OK — {sw.ElapsedMilliseconds}ms";
        }
        catch (Exception ex)
        {
            ResultJson = ex.ToString();
            StatusText = $"❌ Error — {sw.ElapsedMilliseconds}ms";
        }
        finally { IsExecuting = false; }
    }
}
```

---

## Flujo de Uso

```
1. Usuario carga/edita mcp-servers.json
                │
2. App conecta a cada server (stdio spawn ó SSE connect)
                │
3. App llama tools/list → muestra árbol de tools con schemas
                │
4. Usuario selecciona tool → ve descripción + inputSchema
                │
5. Usuario rellena parámetros (form dinámico ó JSON raw)
                │
6. Click "Ejecutar" → tools/call → muestra resultado + timing
                │
7. Historial de llamadas en panel de log (opcional)
```

---

## Recomendación de Enfoque Óptimo

**Fase 1 (MVP rápido)**:
- JSON raw como editor de parámetros (AvalonEdit con syntax highlighting)
- Un solo panel de resultado con JSON formateado
- Config desde archivo JSON

**Fase 2 (mejoras)**:
- Formulario dinámico auto-generado desde `inputSchema`
- Historial de llamadas con replay
- Tabs para múltiples servers simultáneos
- Export/import de test cases (tool + params guardados)

**Fase 3 (pro)**:
- Test suites (secuencias de llamadas)
- Diff entre respuestas
- Variables de entorno en parámetros (`{{timestamp}}`)

El SDK `ModelContextProtocol` de Microsoft maneja toda la complejidad del protocolo (handshake, capabilities, serialización JSON-RPC). Tu app solo necesita orquestar la UI alrededor de `ListToolsAsync()` y `CallToolAsync()` — son literalmente las dos únicas llamadas que importan para testing.