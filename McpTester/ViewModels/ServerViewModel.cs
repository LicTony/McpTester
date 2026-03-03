using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using McpDotNet.Protocol.Types;

namespace McpTester.ViewModels;

public partial class ServerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private ObservableCollection<Tool> _tools = new();

    public ServerViewModel(string name, IList<Tool> tools)
    {
        _name = name;
        _isConnected = true;
        foreach (var t in tools)
            _tools.Add(t);
    }
}
