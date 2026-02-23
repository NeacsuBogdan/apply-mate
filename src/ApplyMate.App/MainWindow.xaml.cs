using ApplyMate.App.Services.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace ApplyMate.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        var trayIconService = App.Services.GetRequiredService<ITrayIconService>();
        trayIconService.Dispose();
    }
}
