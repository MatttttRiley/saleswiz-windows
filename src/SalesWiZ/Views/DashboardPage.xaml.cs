using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace SalesWiZ.Views;

public sealed class IndustryRow
{
    public string Initial { get; set; } = "";
    public string Label { get; set; } = "";
    public string Detail { get; set; } = "";
}

public sealed partial class DashboardPage : Page
{
    private App ApiOwner => (App)Application.Current;

    public DashboardPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        ApiOwner.MainWindowInstance?.ShowLogin();
    }

    private async Task LoadAsync()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ContentScroll.Visibility = Visibility.Collapsed;

        try
        {
            var d = await ApiOwner.Api.FetchDashboardAsync();
            Fill(d);
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

    private void Fill(Dashboard d)
    {
        var progress = d.Progress;
        ProgressBar.Value = progress?.Percent ?? 0;
        ProgressText.Text = $"{progress?.Percent ?? 0}% — {progress?.Stage ?? "Outreach"}";

        if (d.Eta is { } eta)
        {
            EtaHeadline.Text = $"First customer in {eta.RangeText}";
            EtaLikely.Text = eta.LikelyText;
            EtaPaths.Text = eta.EmailDays is not null && eta.CallDays is not null
                ? $"Email path {eta.EmailDays.Text} · Call path {eta.CallDays.Text}"
                : "";
            StallWarning.Visibility = eta.Stalled == true ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            EtaHeadline.Text = "First customer in —";
            EtaLikely.Text = "";
            EtaPaths.Text = "";
            StallWarning.Visibility = Visibility.Collapsed;
        }

        LeadsValue.Text = $"{d.Total ?? 0}";
        SentValue.Text = $"{d.Sent ?? 0}";
        DeliveredValue.Text = $"{d.Delivered ?? 0}";
        RepliesValue.Text = $"{d.Replies ?? 0}";
        InterestedValue.Text = $"{d.Interested ?? 0}";
        DemosValue.Text = $"{d.Demos ?? 0}";
        CustomersValue.Text = $"{d.Customers ?? 0}";
        BouncedValue.Text = $"{d.Bounced ?? 0}";
        NotContactedValue.Text = $"{d.NotContacted ?? 0}";

        SetupCollectedValue.Text = $"${d.Money?.SetupCollected ?? 0}";
        MrrValue.Text = $"${d.Money?.Mrr ?? 0}/mo";
        PriceValue.Text = $"${d.Money?.SetupFee ?? 500} setup · ${d.Money?.MonthlyFee ?? 100}/mo";

        CallsLeftValue.Text = $"{d.CallsLeftToday ?? 0} of 10";
        FollowupsDueValue.Text = $"{d.FollowupsDue ?? 0}";
        InCallQueueValue.Text = $"{d.InCallQueue ?? 0}";
        RemainingEmailValue.Text = $"{d.RemainingEmail ?? 0}";

        if (d.ByIndustry is { Count: > 0 })
        {
            var rows = d.ByIndustry
                .OrderByDescending(kv => kv.Value.Total ?? 0)
                .Select(kv => new IndustryRow
                {
                    Initial = IndustryInfo.Initial(kv.Key),
                    Label = IndustryInfo.Label(kv.Key),
                    Detail = $"{kv.Value.Delivered ?? 0} sent · {kv.Value.Replies ?? 0} replies",
                })
                .ToList();
            ByIndustryList.ItemsSource = rows;
            ByIndustryCard.Visibility = Visibility.Visible;
        }
        else
        {
            ByIndustryCard.Visibility = Visibility.Collapsed;
        }
    }
}
