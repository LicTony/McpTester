using System.Collections.Generic;
using System.Text.Json;
using System.Text.Encodings.Web;
using McpTester.Models;

namespace McpTester.Services;

/// <summary>
/// Parsea el inputSchema JSON de una tool MCP y genera una lista de <see cref="ToolParameterField"/>
/// listos para binding en la UI de formulario dinámico.
/// </summary>
public static class ToolFormBuilder
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Construye la lista de campos del formulario a partir del inputSchema de la tool.
    /// Recibe directamente un JsonElement del SDK de ModelContextProtocol.
    /// Retorna lista vacía si el schema no tiene "properties".
    /// </summary>
    public static IList<ToolParameterField> BuildFields(JsonElement? inputSchema)
    {
        var fields = new List<ToolParameterField>();

        if (inputSchema is null)
            return fields;

        JsonElement schema = inputSchema.Value;

        // Leer "required" → HashSet para lookup O(1)
        var required = new HashSet<string>();
        if (schema.TryGetProperty("required", out var reqArr))
        {
            foreach (var r in reqArr.EnumerateArray())
            {
                var s = r.GetString();
                if (s is not null) required.Add(s);
            }
        }

        // Leer "properties"
        if (!schema.TryGetProperty("properties", out var props))
            return fields;

        foreach (var prop in props.EnumerateObject())
        {
            string propName = prop.Name;
            bool isRequired = required.Contains(propName);

            string description = "";
            if (prop.Value.TryGetProperty("description", out var desc))
                description = desc.GetString() ?? "";

            // Determinar tipo lógico del campo
            string fieldType = "string";
            IList<string>? enumValues = null;

            // ¿Tiene enum? Prioridad sobre el tipo base
            if (prop.Value.TryGetProperty("enum", out var enumArr))
            {
                fieldType = "enum";
                enumValues = new List<string>();
                foreach (var v in enumArr.EnumerateArray())
                {
                    var vs = v.GetString();
                    if (vs is not null) enumValues.Add(vs);
                    else enumValues.Add(v.ToString());
                }
            }
            else if (prop.Value.TryGetProperty("type", out var typeEl))
            {
                // El tipo puede ser un string o un array ["string", "null"]
                if (typeEl.ValueKind == JsonValueKind.Array)
                {
                    // Tomar el primer tipo que no sea "null"
                    foreach (var t in typeEl.EnumerateArray())
                    {
                        var ts = t.GetString();
                        if (ts is not null && ts != "null")
                        {
                            fieldType = ts;
                            break;
                        }
                    }
                }
                else
                {
                    fieldType = typeEl.GetString() ?? "string";
                }
            }

            // Normalización
            fieldType = fieldType switch
            {
                "integer" or "number" => fieldType,
                "boolean"             => "boolean",
                "array"               => "array",
                "object"              => "object",
                "enum"                => "enum",
                _                     => "string"
            };

            var field = new ToolParameterField
            {
                Name        = propName,
                Label       = isRequired ? $"{propName} *" : propName,
                Description = description,
                FieldType   = fieldType,
                IsRequired  = isRequired,
                EnumValues  = enumValues,
            };

            // Para enums: preseleccionar primer valor
            if (fieldType == "enum" && enumValues is { Count: > 0 })
                field.SelectedEnumValue = enumValues[0];

            fields.Add(field);
        }

        return fields;
    }

    /// <summary>
    /// Recolecta los valores actuales del formulario en un Dictionary listo para CallToolAsync.
    /// Omite campos vacíos no requeridos.
    /// </summary>
    public static Dictionary<string, object?> CollectValues(IEnumerable<ToolParameterField> fields)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var field in fields)
        {
            var value = field.GetValue();

            // Incluir siempre campos requeridos; opcionales solo si tienen valor
            if (value is not null || field.IsRequired)
            {
                dict[field.Name] = value;
            }
        }

        return dict;
    }
}
