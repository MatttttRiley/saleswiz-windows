using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SalesWiZ.Views;

public sealed partial class AddLeadDialog : ContentDialog
{
    private readonly ApiClient _api;

    public Lead? CreatedLead { get; private set; }

    public AddLeadDialog()
    {
        // The ApiClient is fetched from the App at show time so the dialog can
        // also be built by the designer without a live client.
        _api = ((App)Application.Current).Api;
        InitializeComponent();
        IndustryCombo.ItemsSource = IndustryInfo.All
            .Select(i => new LeadsPage.FilterOption { Value = i, Label = IndustryInfo.Label(i) })
            .ToList();
        IndustryCombo.SelectedIndex = 0;
    }

    private async void AddLeadDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                ErrorText.Text = "Enter the business name.";
                ErrorText.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }
            CreatedLead = await _api.AddLeadAsync(
                NameBox.Text.Trim(),
                IndustryCombo.SelectedValue as string,
                OrNull(CityBox.Text),
                OrNull(PhoneBox.Text),
                OrNull(EmailBox.Text),
                OrNull(ContactBox.Text),
                OrNull(NotesBox.Text));
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private static string? OrNull(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
