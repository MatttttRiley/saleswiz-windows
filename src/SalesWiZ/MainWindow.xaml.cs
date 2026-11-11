using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SalesWiZ.Views;

namespace SalesWiZ;

public sealed partial class MainWindow : Window
{
    private readonly App _app;
    private bool _isNavigating;

    public MainWindow(App app)
    {
        _app = app;
        InitializeComponent();
        Title = "SalesWiZ";
        _ = TrySilentLoginAsync();
    }

    private async Task TrySilentLoginAsync()
    {
        if (await _app.Api.TrySilentLoginAsync())
        {
            ShowShell("Dashboard");
        }
        else
        {
            // Stay on the login layer; the user types the password once.
            LoginControl.FocusPassword();
        }
    }

    private void LoginControl_LoginSucceeded(object sender, EventArgs e)
    {
        ShowShell("Dashboard");
    }

    private void ShowShell(string startPage)
    {
        LoginLayer.Visibility = Visibility.Collapsed;
        NavView.Visibility = Visibility.Visible;
        NavigateTo(startPage);
    }

    private void NavigateTo(string tag)
    {
        Type? pageType = tag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "Leads" => typeof(LeadsPage),
            "Inbox" => typeof(InboxPage),
            "Interest" => typeof(InterestPage),
            "Actions" => typeof(ActionsPage),
            _ => null,
        };
        if (pageType == null) return;

        NavHeaderText.Text = tag;
        _isNavigating = true;
        try
        {
            foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
            {
                if ((string?)item.Tag == tag) NavView.SelectedItem = item;
            }
        }
        finally
        {
            _isNavigating = false;
        }
        ContentFrame.Navigate(pageType);
    }

    /// <summary>Log out and show the login layer (used by the shell and pages).</summary>
    public void ShowLogin()
    {
        _app.Api.Logout();
        ContentFrame.Content = null;
        NavView.Visibility = Visibility.Collapsed;
        LoginLayer.Visibility = Visibility.Visible;
        LoginControl.Reset();
        LoginControl.FocusPassword();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_isNavigating) return;
        if (args.SelectedItem is not NavigationViewItem item) return;
        var tag = (string?)item.Tag;
        if (tag == "SignOut")
        {
            ShowLogin();
            return;
        }
        if (tag != null) NavigateTo(tag);
    }
}
