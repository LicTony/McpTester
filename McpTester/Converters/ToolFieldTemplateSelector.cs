using System.Windows;
using System.Windows.Controls;
using McpTester.Models;

namespace McpTester.Converters;

/// <summary>
/// DataTemplateSelector que elige el DataTemplate correcto para un <see cref="ToolParameterField"/>
/// según su FieldType (string, integer, boolean, enum, array/object).
/// Los DataTemplates se definen como recursos en MainWindow.xaml.
/// </summary>
public class ToolFieldTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate      { get; set; }
    public DataTemplate? BoolTemplate      { get; set; }
    public DataTemplate? EnumTemplate      { get; set; }
    public DataTemplate? MultilineTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not ToolParameterField field)
            return base.SelectTemplate(item, container);

        return field.FieldType switch
        {
            "boolean"          => BoolTemplate,
            "enum"             => EnumTemplate,
            "array" or "object"=> MultilineTemplate,
            _                  => TextTemplate   // string, integer, number
        };
    }
}
