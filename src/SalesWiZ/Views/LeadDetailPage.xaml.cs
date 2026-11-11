using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace SalesWiZ.Views;

public sealed partial class LeadDetailPage : Page
{
    private App ApiOwner => (App)Application.Current;

    private int _leadId;
    private Lead? _lead;

    public LeadDetailPage()
    {
        InitializeComponent();
        StatusCombo.ItemsSource = LeadStatusInfo.Settable
            .Select(s => new LeadsPage.FilterOption { Value = s, Label = LeadStatusInfo.Label(s) })
            .ToList();
    }

    public static string PrettyDate(string? value) => Helpers.PrettyDate(value);
    public static SolidColorBrush EmailBadgeBrush(string hex) => Helpers.BadgeBrush(hex);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is int id)
        {
            _leadId = id;
            await LoadAsync();
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ContentScroll.Visibility = Visibility.Collapsed;
        ActionMessage.Visibility = Visibility.Collapsed;

        try
        {
            _lead = await ApiOwner.Api.FetchLeadAsync(_leadId);
            Fill(_lead);
            var emails = await ApiOwner.Api.FetchEmailsAsync(_leadId, limit: 50);
            EmailsList.ItemsSource = emails.Emails;
            EmailsCard.Visibility = emails.Emails.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            LoadingPanel.Visibility = Visibility.Collapsed;
            ContentScroll.Visibility = Visibility.Visible;
        }
        catch (UnauthorizedException)
        {
            ApiOwner.MainWindowInstance?.ShowLogin();
        }
        catch (Exception ex)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorText.Text = ex.Message;
            ErrorPanel.Visibility = Visibility.Visible;
        }
    }

    private void Fill(Lead lead)
    {
        NameText.Text = lead.Name;
        StatusText.Text = LeadStatusInfo.Label(lead.Status);
        StatusBadge.Background = Helpers.StatusBrush(lead.Status);
        SubtitleText.Text = lead.DisplayCity;

        PhoneText.Text = string.IsNullOrEmpty(lead.Phone) ? "No phone" : lead.Phone;
        CallButton.Visibility = lead.TelUri is not null ? Visibility.Visible : Visibility.Collapsed;
        EmailText.Text = string.IsNullOrEmpty(lead.Email) ? "No email" : lead.Email;
        MailButton.Visibility = string.IsNullOrEmpty(lead.Email) ? Visibility.Collapsed : Visibility.Visible;

        ContactPersonText.Text = string.IsNullOrEmpty(lead.ContactPerson)
            ? "" : $"Contact: {lead.ContactPerson}";
        ContactPersonText.Visibility = string.IsNullOrEmpty(lead.ContactPerson)
            ? Visibility.Collapsed : Visibility.Visible;
        SourceText.Text = string.IsNullOrEmpty(lead.Source) ? "" : $"Source: {lead.Source}";
        SourceText.Visibility = string.IsNullOrEmpty(lead.Source)
            ? Visibility.Collapsed : Visibility.Visible;

        var dates = new List<string>();
        if (!string.IsNullOrEmpty(lead.DateEmailed)) dates.Add($"Emailed {Helpers.PrettyDate(lead.DateEmailed)}");
        if (!string.IsNullOrEmpty(lead.DateCalled)) dates.Add($"Called {Helpers.PrettyDate(lead.DateCalled)}");
        if (!string.IsNullOrEmpty(lead.DateReplied)) dates.Add($"Replied {Helpers.PrettyDate(lead.DateReplied)}");
        if (!string.IsNullOrEmpty(lead.CallOutcome)) dates.Add($"Last call: {lead.CallOutcome}");
        DatesText.Text = string.Join(" · ", dates);
        DatesText.Visibility = dates.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        StatusCombo.SelectedValue = lead.Status;

        NotesText.Text = string.IsNullOrEmpty(lead.Notes) ? "No notes." : lead.Notes;

        HistoryList.ItemsSource = lead.History ?? [];
    }

    private async void SaveStatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (StatusCombo.SelectedValue is not string status || status.Length == 0) return;
        try
        {
            _lead = await ApiOwner.Api.UpdateLeadStatusAsync(_leadId, status);
            Fill(_lead);
            ShowActionMessage($"Status set to {LeadStatusInfo.Label(status)}.");
        }
        catch (Exception ex)
        {
            ShowActionMessage(ex.Message);
        }
    }

    private async void LogCallButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LogCallDialog { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            _lead = await ApiOwner.Api.LogCallAsync(_leadId, dialog.Outcome, dialog.Note, dialog.FollowUpDue);
            Fill(_lead);
            ShowActionMessage($"Call logged: {dialog.Outcome}.");
        }
        catch (Exception ex)
        {
            ShowActionMessage(ex.Message);
        }
    }

    private async void LogReplyButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LogReplyDialog { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            _lead = await ApiOwner.Api.LogReplyAsync(_leadId, dialog.Note);
            Fill(_lead);
            ShowActionMessage("Reply logged.");
        }
        catch (Exception ex)
        {
            ShowActionMessage(ex.Message);
        }
    }

    private async void BounceButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Mark bounced?",
            Content = "This marks the lead bounced and do-not-contact. It cannot be undone.",
            PrimaryButtonText = "Mark bounced",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            _lead = await ApiOwner.Api.RecordBounceAsync(_leadId, reason: null);
            Fill(_lead);
            ShowActionMessage("Marked bounced.");
        }
        catch (Exception ex)
        {
            ShowActionMessage(ex.Message);
        }
    }

    private async void CallButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lead?.TelUri is string uri)
        {
            _ = await Launcher.LaunchUriAsync(new Uri(uri));
        }
    }

    private async void MailButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_lead?.Email))
        {
            _ = await Launcher.LaunchUriAsync(new Uri($"mailto:{_lead.Email}"));
        }
    }

    private void EmailsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is EmailMessage email)
        {
            Frame.Navigate(typeof(EmailDetailPage), email);
        }
    }

    private void ShowActionMessage(string message)
    {
        ActionMessage.Text = message;
        ActionMessage.Visibility = Visibility.Visible;
    }
}
