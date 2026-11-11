using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace SalesWiZ.Views;

public sealed partial class LeadsPage : Page
{
    private App ApiOwner => (App)Application.Current;

    private readonly List<Lead> _leads = [];
    private int _offset;
    private int _total;
    private bool _loading;

    public sealed class FilterOption
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public LeadsPage()
    {
        InitializeComponent();
        IndustryFilter.ItemsSource = new[] { new FilterOption { Value = "", Label = "All industries" } }
            .Concat(IndustryInfo.All.Select(i => new FilterOption { Value = i, Label = IndustryInfo.Label(i) }))
            .ToList();
        IndustryFilter.DisplayMemberPath = "Label";
        IndustryFilter.SelectedValuePath = "Value";
        IndustryFilter.SelectedIndex = 0;
        StatusFilter.ItemsSource = new[] { new FilterOption { Value = "", Label = "All statuses" } }
            .Concat(LeadStatusInfo.All.Select(s => new FilterOption { Value = s, Label = LeadStatusInfo.Label(s) }))
            .ToList();
        StatusFilter.DisplayMemberPath = "Label";
        StatusFilter.SelectedValuePath = "Value";
        StatusFilter.SelectedIndex = 0;
    }

    public static string Initial(string? industry) => IndustryInfo.Initial(industry);
    public static string StatusLabel(string status) => LeadStatusInfo.Label(status);
    public static SolidColorBrush StatusBrush(string status) => Helpers.StatusBrush(status);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (_leads.Count == 0) await LoadAsync(reset: true);
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e) => await LoadAsync(reset: true);

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) await LoadAsync(reset: true);
    }

    private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await LoadAsync(reset: true);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadAsync(reset: true);

    private async void LoadMoreButton_Click(object sender, RoutedEventArgs e) => await LoadAsync(reset: false);

    private async void AddLeadButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddLeadDialog { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (dialog.CreatedLead is null) return;
        await LoadAsync(reset: true);
        Frame.Navigate(typeof(LeadDetailPage), dialog.CreatedLead.Id);
    }

    private void LeadsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Lead lead)
        {
            Frame.Navigate(typeof(LeadDetailPage), lead.Id);
        }
    }

    private string? SelectedIndustry =>
        IndustryFilter.SelectedValue is string s && s.Length > 0 ? s : null;

    private string? SelectedStatus =>
        StatusFilter.SelectedValue is string s && s.Length > 0 ? s : null;

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
            _leads.Clear();
            LeadsList.ItemsSource = null;
        }

        try
        {
            var response = await ApiOwner.Api.FetchLeadsAsync(
                SearchBox.Text.Trim(), SelectedIndustry, SelectedStatus,
                limit: 100, offset: _offset);
            _leads.AddRange(response.Leads);
            _offset = _leads.Count;
            _total = response.Total;
            LeadsList.ItemsSource = _leads.ToList();
            CountText.Text = $"{_leads.Count} of {_total} leads";
            LoadMoreButton.Visibility = _leads.Count < _total ? Visibility.Visible : Visibility.Collapsed;
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
