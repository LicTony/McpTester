using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpTester.Models;
using McpTester.Services;
using Microsoft.Win32;
using McpDotNet.Protocol.Types;

namespace McpTester.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly McpConnectionService _mcp = new();

    [ObservableProperty]
    private ObservableCollection<ServerViewModel> _servers = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteToolCommand))]
    private ServerViewModel? _selectedServer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteToolCommand))]
    private Tool? _selectedTool;

    [ObservableProperty]
    private string _parameterJson = "{}";

    [ObservableProperty]
    private string _resultJson = "";

    [ObservableProperty]
    private string _statusText = "Listo. Cargue una configuración para comenzar.";

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _logText = "";

    [ObservableProperty]
    private string _toolDescription = "";

    [ObservableProperty]
    private string _toolName = "";

    [ObservableProperty]
    private string _toolSchemaJson = "";

    /// <summary>Campos del formulario dinámico generados desde el inputSchema.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFormFields))]
    private ObservableCollection<ToolParameterField> _formFields = new();

    /// <summary>True cuando hay campos de formulario disponibles para la tool seleccionada.</summary>
    public bool HasFormFields => FormFields.Count > 0;

    partial void OnSelectedToolChanged(Tool? value)
    {
        FormFields.Clear();
        OnPropertyChanged(nameof(HasFormFields));

        if (value is null)
        {
            ToolName = "";
            ToolDescription = "";
            ToolSchemaJson = "";
            return;
        }

        ToolName = value.Name;
        ToolDescription = value.Description ?? "(Sin descripción)";

        try
        {
            ToolSchemaJson = JsonSerializer.Serialize(value.InputSchema, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch
        {
            ToolSchemaJson = "{}";
        }

        // Generar campos del formulario dinámico
        try
        {
            var fields = ToolFormBuilder.BuildFields(value.InputSchema);
            foreach (var f in fields)
                FormFields.Add(f);
                
            OnPropertyChanged(nameof(HasFormFields));
        }
        catch
        {
            // Si el schema no es parseable, el formulario queda vacío → se usa JSON raw
        }
    }

    private void AppendLog(string message)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        LogText = string.IsNullOrEmpty(LogText)
            ? $"[{ts}] {message}"
            : $"{LogText}\n[{ts}] {message}";
    }

    [RelayCommand]
    private async Task LoadConfigAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar configuración MCP",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = "mcp-servers.json"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            StatusText = "Conectando a servidores...";
            _mcp.DisconnectAll();
            Servers.Clear();
            SelectedTool = null;

            var json = await File.ReadAllTextAsync(dialog.FileName);
            var config = JsonSerializer.Deserialize<McpConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (config?.Servers is null || config.Servers.Count == 0)
            {
                StatusText = "⚠️ No se encontraron servidores en el archivo.";
                return;
            }

            foreach (var (name, serverConfig) in config.Servers)
            {
                try
                {
                    AppendLog($"Conectando a '{name}'...");

                    if (serverConfig.GetTransportType() == TransportType.Sse)
                        await _mcp.ConnectSseAsync(name, serverConfig);
                    else
                        await _mcp.ConnectStdioAsync(name, serverConfig);

                    var tools = await _mcp.ListToolsAsync(name);
                    Servers.Add(new ServerViewModel(name, tools));
                    AppendLog($"✅ '{name}' conectado — {tools.Count} tool(s)");
                }
                catch (Exception ex)
                {
                    AppendLog($"❌ Error conectando '{name}': {ex.Message}");
                }
            }

            StatusText = $"{Servers.Count} servidor(es) conectado(s).";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Error cargando configuración: {ex.Message}";
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        _mcp.DisconnectAll();
        Servers.Clear();
        SelectedTool = null;
        ResultJson = "";
        StatusText = "Desconectado.";
        AppendLog("Desconectado de todos los servidores.");
    }

    [RelayCommand(CanExecute = nameof(CanExecuteTool))]
    private async Task ExecuteToolAsync()
    {
        if (SelectedTool is null || SelectedServer is null) return;

        IsExecuting = true;
        ExecuteToolCommand.NotifyCanExecuteChanged();
        var sw = Stopwatch.StartNew();

        try
        {
            Dictionary<string, object?> args;

            if (HasFormFields)
            {
                // Recolectar valores del formulario dinámico
                args = ToolFormBuilder.CollectValues(FormFields);
            }
            else
            {
                // Fallback: usar el editor JSON raw
                try
                {
                    args = JsonSerializer.Deserialize<Dictionary<string, object?>>(ParameterJson)
                           ?? new Dictionary<string, object?>();
                }
                catch (JsonException)
                {
                    StatusText = "❌ JSON de parámetros inválido.";
                    return;
                }
            }

            AppendLog($"→ {SelectedServer.Name}/{SelectedTool.Name}");
            var result = await _mcp.CallToolAsync(SelectedServer.Name, SelectedTool.Name, args);
            sw.Stop();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // Convertimos el resultado a un JsonNode para poder manipularlo dinámicamente
            var resultNode = JsonSerializer.SerializeToNode(result, options);
            
            if (resultNode is JsonObject resultObj && resultObj.TryGetPropertyValue("content", out var contentNode) && contentNode is JsonArray contentArray)
            {
                foreach (var item in contentArray)
                {
                    if (item is JsonObject itemObj && itemObj.TryGetPropertyValue("type", out var typeNode) && typeNode?.ToString() == "text" 
                        && itemObj.TryGetPropertyValue("text", out var textNode) && textNode is JsonValue textVal)
                    {
                        string? rawText = textVal.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(rawText))
                        {
                            try
                            {
                                // Si el texto es un JSON válido, lo reemplazamos por el objeto JSON real
                                // Esto evita que se serialice como una cadena escapada
                                var nestedJson = JsonNode.Parse(rawText);
                                itemObj["text"] = nestedJson;
                            }
                            catch { /* No es JSON, lo dejamos como está */ }
                        }
                    }
                }
            }

            ResultJson = resultNode?.ToJsonString(options) ?? "";
            StatusText = $"✅ OK — {sw.ElapsedMilliseconds} ms";
            AppendLog($"← ✅ OK ({sw.ElapsedMilliseconds} ms)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            ResultJson = ex.ToString();
            StatusText = $"❌ Error — {sw.ElapsedMilliseconds} ms";
            AppendLog($"← ❌ {ex.Message}");
        }
        finally
        {
            IsExecuting = false;
            ExecuteToolCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanExecuteTool() => SelectedTool is not null && SelectedServer is not null && !IsExecuting;
}
