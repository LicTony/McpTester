using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;
using McpTester.ViewModels;
using ModelContextProtocol.Protocol;

namespace McpTester;

public partial class MainWindow : Window
{
    private MainViewModel _vm = null!;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        // Configurar AvalonEdit: editor de parámetros
        ParamEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
        ParamEditor.Text = "{}";
        ParamEditor.TextChanged += (_, _) =>
        {
            _vm.ParameterJson = ParamEditor.Text;
        };

        // Configurar AvalonEdit: resultado y esquema (readonly)
        ResultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
        SchemaEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");

        // Sincronizar ViewModel → Editores
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ResultJson))
            {
                ResultEditor.Text = _vm.ResultJson;
            }

            if (e.PropertyName == nameof(MainViewModel.ToolSchemaJson))
            {
                SchemaEditor.Text = _vm.ToolSchemaJson;
            }

            // Scroll automático del log
            if (e.PropertyName == nameof(MainViewModel.LogText))
            {
                LogScrollViewer.ScrollToEnd();
            }

            // Auto-seleccionar pestaña según disponibilidad del formulario dinámico
            if (e.PropertyName == nameof(MainViewModel.HasFormFields))
            {
                // Índice 0 = Formulario, Índice 1 = Argumentos (JSON), Índice 2 = Esquema Técnico
                ParamTabControl.SelectedIndex = _vm.HasFormFields ? 0 : 1;
            }
        };
    }

    private void TreeServers_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is Tool tool)
        {
            // Buscar el server al que pertenece esta tool
            foreach (var server in _vm.Servers)
            {
                foreach (var t in server.Tools)
                {
                    if (t == tool)
                    {
                        _vm.SelectedServer = server;
                        _vm.SelectedTool = tool;

                        // Resetear parámetros
                        ParamEditor.Text = "{}";
                        ResultEditor.Text = "";
                        return;
                    }
                }
            }
        }
        else
        {
            // Seleccionó un servidor, no una tool
            _vm.SelectedTool = null;
        }
    }
}