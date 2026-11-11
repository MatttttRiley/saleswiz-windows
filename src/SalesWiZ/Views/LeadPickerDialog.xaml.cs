using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace SalesWiZ.Views;

public sealed partial class LeadPickerDialog : ContentDialog
{
    private readonly ApiClient _api;

    public Lead? SelectedLead { get; private set; }

    public LeadPickerDialog(ApiClient api)
    {
        _api = api;
        InitializeComponent();
        IsPrimaryButtonEnabled = false;
        _ = LoadAsync("");
    }

    private void LeadsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedLead = LeadsList.SelectedItem as Lead;
        IsPrimaryButtonEnabled = SelectedLead is not null;
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e) =>
        await LoadAsync(SearchBox.Text.Trim());

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) await LoadAsync(SearchBox.Text.Trim());
    }

    private async Task LoadAsync(string search)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            var response = await _api.FetchLeadsAsync(search, industry: null, status: null, limit: 100);
            LeadsList.ItemsSource = response.Leads;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
