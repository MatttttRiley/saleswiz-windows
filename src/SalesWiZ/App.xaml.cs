using Microsoft.UI.Xaml;

namespace SalesWiZ;

/// <summary>
/// SalesWiZ — the Riley AI Consulting CRM on your PC.
/// Same data as the web CRM (https://crm.129-146-5-140.sslip.io/), same
/// login (the shared CRM password), no web views.
/// </summary>
public partial class App : Application
{
    /// <summary>Single shared API client for the whole app (one cookie jar).</summary>
    public ApiClient Api { get; } = new ApiClient();

    private Window? _window;

    public MainWindow? MainWindowInstance => _window as MainWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow(this);
        _window.Activate();
    }
}
