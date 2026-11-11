using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace SalesWiZ.Views;

public sealed partial class InboxPage : Page
{
    private App ApiOwner => (App)Application.Current;

    private readonly List<EmailMessage> _emails = [];
    private int _offset;
    private int _total;
    private bool _loading;

    public InboxPage()
    {
        InitializeComponent();
    }

    public static string PrettyDateTime(string? value) => Helpers.PrettyDateTime(value);
    public static SolidColorBrush EmailBadgeBrush(string hex) => Helpers.BadgeBrush(hex);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (_emails.Count == 0) await LoadAsync(reset: true);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadAsync(reset: true);

    private async void LoadMoreButton_Click(object sender, RoutedEventArgs e) => await LoadAsync(reset: false);

    private async void ComposeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ComposeDialog(ApiOwner.Api) { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await LoadAsync(reset: true);
        }
    }

    private void EmailsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is EmailMessage email)
        {
            Frame.Navigate(typeof(EmailDetailPage), email);
        }
    }

    private async Task LoadAsync(bool reset)
    {
        if (_loading) return;
        _loading = true;
        BusyRing.IsActive = true;
        BusyRing.Visibility = Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;

        if (reset)
        {
            _offset = 0;
            _emails.Clear();
            EmailsList.ItemsSource = null;
        }

        try
        {
            var response = await ApiOwner.Api.FetchEmailsAsync(limit: 50, offset: _offset);
            _emails.AddRange(response.Emails);
            _offset = _emails.Count;
            _total = response.Total;
            EmailsList.ItemsSource = _emails.ToList();
            CountText.Text = $"{_emails.Count} of {_total} emails";
            LoadMoreButton.Visibility = _emails.Count < _total ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (UnauthorizedException)
        {
            ApiOwner.MainWindowInstance?.ShowLogin();
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _loading = false;
            BusyRing.IsActive = false;
            BusyRing.Visibility = Visibility.Collapsed;
        }
    }
}
