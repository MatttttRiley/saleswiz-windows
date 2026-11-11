using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace SalesWiZ.Views;

public sealed partial class EmailDetailPage : Page
{
    private App ApiOwner => (App)Application.Current;

    private EmailMessage? _email;

    public EmailDetailPage()
    {
        InitializeComponent();
    }

    public static string PrettyDateTime(string? value) => Helpers.PrettyDateTime(value);
    public static SolidColorBrush EmailBadgeBrush(string hex) => Helpers.BadgeBrush(hex);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is EmailMessage email)
        {
            _email = email;
            Fill(email);
            ContentScroll.Visibility = Visibility.Visible;
            LoadingPanel.Visibility = Visibility.Collapsed;
            await LoadThreadAsync(email);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void Fill(EmailMessage email)
    {
        SubjectText.Text = string.IsNullOrEmpty(email.Subject) ? "(no subject)" : email.Subject;
        BadgeText.Text = email.BadgeLabel;
        BadgeBorder.Background = Helpers.BadgeBrush(email.BadgeColorHex);
        DateText.Text = Helpers.PrettyDateTime(email.ReceivedAt);
        FromText.Text = $"From: {email.FromAddr ?? ""}";
        ToText.Text = $"To: {email.ToAddr ?? ""}";
        BodyText.Text = email.Body ?? "";
        ReplyButton.Visibility = email.IsInbound ? Visibility.Visible : Visibility.Collapsed;
        LeadButton.Visibility = email.LeadId is not null ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task LoadThreadAsync(EmailMessage email)
    {
        if (email.LeadId is null)
        {
            ThreadCard.Visibility = Visibility.Collapsed;
            return;
        }
        try
        {
            var response = await ApiOwner.Api.FetchEmailsAsync(email.LeadId, limit: 50);
            var thread = response.Emails.Where(m => m.Id != email.Id).ToList();
            ThreadList.ItemsSource = thread;
            ThreadCard.Visibility = thread.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch
        {
            ThreadCard.Visibility = Visibility.Collapsed;
        }
    }

    private async void ReplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_email is null) return;
        var dialog = new ReplyDialog(ApiOwner.Api, _email) { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await LoadThreadAsync(_email);
        }
    }

    private void LeadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_email?.LeadId is int leadId)
        {
            Frame.Navigate(typeof(LeadDetailPage), leadId);
        }
    }

    private void ThreadList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is EmailMessage email)
        {
            Frame.Navigate(typeof(EmailDetailPage), email);
        }
    }
}
