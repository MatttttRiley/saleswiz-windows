using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace SalesWiZ.Views;

public sealed partial class InterestPage : Page
{
    private App ApiOwner => (App)Application.Current;

    public InterestPage()
    {
        InitializeComponent();
    }

    public static string Initial(string? industry) => IndustryInfo.Initial(industry);
    public static string RepliedText(string? dateReplied) => Helpers.RepliedText(dateReplied);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void InterestList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Lead lead)
        {
            Frame.Navigate(typeof(LeadDetailPage), lead.Id);
        }
    }

    private async Task LoadAsync()
    {
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            var response = await ApiOwner.Api.FetchInterestedLeadsAsync();
            InterestList.ItemsSource = response.Leads;
            CountText.Text = $"{response.Total} interested";
            EmptyText.Visibility = response.Leads.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
    }
}
