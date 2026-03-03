using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;
using McpTester.ViewModels;
using McpDotNet.Protocol.Types;

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

        // Configurar AvalonEdit: resultado (readonly)
        ResultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");

        // Sincronizar ResultJson → ResultEditor
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ResultJson))
            {
                ResultEditor.Text = _vm.ResultJson;
            }

            // Scroll automático del log
            if (e.PropertyName == nameof(MainViewModel.LogText))
            {
                LogScrollViewer.ScrollToEnd();
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