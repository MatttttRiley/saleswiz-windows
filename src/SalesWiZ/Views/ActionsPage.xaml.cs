using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace SalesWiZ.Views;

public sealed partial class ActionsPage : Page
{
    private App ApiOwner => (App)Application.Current;

    public ActionsPage()
    {
        InitializeComponent();
    }

    public static string? DueText(string? followUpDue) => Helpers.DueText(followUpDue);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void LeadList_ItemClick(object sender, ItemClickEventArgs e)
    {
        int? id = e.ClickedItem switch
        {
            FollowupLead f => f.Id,
            CallQueueLead c => c.Id,
            BatchLead b => b.Id,
            _ => null,
        };
        if (id is not null) Frame.Navigate(typeof(LeadDetailPage), id.Value);
    }

    private async void CallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string uri } && uri.Length > 0)
        {
            _ = await Launcher.LaunchUriAsync(new Uri(uri));
        }
    }

    private async void QueueBatchButton_Click(object sender, RoutedEventArgs e)
    {
        QueueMessage.Visibility = Visibility.Collapsed;
        try
        {
            var result = await ApiOwner.Api.QueueBatchAsync();
            QueueMessage.Text = $"Batch {result.Batch ?? 0}: {result.Queued ?? 0} leads queued.";
            QueueMessage.Visibility = Visibility.Visible;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            QueueMessage.Text = ex.Message;
            QueueMessage.Visibility = Visibility.Visible;
        }
    }

    private async Task LoadAsync()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ContentScroll.Visibility = Visibility.Collapsed;

        try
        {
            var actions = await ApiOwner.Api.FetchActionsAsync();
            CallsLeftText.Text = $"{actions.CallsLeftToday ?? 0} of 10";

            var followups = actions.Followups ?? [];
            FollowupsHeader.Text = $"Follow-ups due ({followups.Count})";
            FollowupsList.ItemsSource = followups;
            FollowupsEmpty.Visibility = followups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var queue = actions.CallQueue ?? [];
            CallQueueHeader.Text = $"Call queue ({queue.Count})";
            CallQueueList.ItemsSource = queue;
            CallQueueEmpty.Visibility = queue.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var batch = actions.NextBatch ?? [];
            NextBatchHeader.Text = $"Next batch ({batch.Count})";
            NextBatchList.ItemsSource = batch;
            NextBatchEmpty.Visibility = batch.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

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
}
