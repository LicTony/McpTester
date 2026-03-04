using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace McpTester.Models;

/// <summary>
/// Representa un campo individual del formulario dinámico generado desde el inputSchema de una tool MCP.
/// </summary>
public partial class ToolParameterField : ObservableObject
{
    /// <summary>Nombre del parámetro en el schema (ej: "path", "encoding").</summary>
    public string Name { get; init; } = "";

    /// <summary>Etiqueta visual. Incluye " *" si el campo es requerido.</summary>
    public string Label { get; init; } = "";

    /// <summary>Descripción del campo para mostrar como tooltip y ayuda.</summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Tipo lógico del campo: "string" | "integer" | "number" | "boolean" | "array" | "object" | "enum"
    /// Determina qué control WPF se renderiza.
    /// </summary>
    public string FieldType { get; init; } = "string";

    /// <summary>Indica si el campo es obligatorio según el schema.</summary>
    public bool IsRequired { get; init; }

    /// <summary>Opciones disponibles cuando FieldType == "enum".</summary>
    public IList<string>? EnumValues { get; init; }

    // ──────── Valor actual del campo ────────

    [ObservableProperty]
    private string _textValue = "";

    [ObservableProperty]
    private bool _boolValue;

    [ObservableProperty]
    private string? _selectedEnumValue;

    /// <summary>
    /// Retorna el valor del campo en el tipo correcto para incluirlo en el Dictionary de args.
    /// Retorna null si el campo está vacío (y no es requerido).
    /// </summary>
    public object? GetValue()
    {
        return FieldType switch
        {
            "boolean" => (object?)BoolValue,
            "enum"    => SelectedEnumValue,
            "integer" => int.TryParse(TextValue, out var i) ? i : (object?)TextValue,
            "number"  => double.TryParse(TextValue,
                             System.Globalization.NumberStyles.Any,
                             System.Globalization.CultureInfo.InvariantCulture,
                             out var d) ? d : (object?)TextValue,
            _         => string.IsNullOrEmpty(TextValue) ? null : TextValue
        };
    }
}
