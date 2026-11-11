using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SalesWiZ.Views;

/// <summary>
/// Compose a new outbound email. Queues it via queue_email — it only sends
/// after Matt approves the send. The recipient can be typed or picked from
/// the leads.
/// </summary>
public sealed partial class ComposeDialog : ContentDialog
{
    private readonly ApiClient _api;
    private int? _leadId;

    public ComposeDialog(ApiClient api)
    {
        _api = api;
        InitializeComponent();
    }

    private async void ChooseLeadButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new LeadPickerDialog(_api) { XamlRoot = XamlRoot };
        if (await picker.ShowAsync() != ContentDialogResult.Primary) return;
        if (picker.SelectedLead is null) return;
        _leadId = picker.SelectedLead.Id;
        LeadText.Text = picker.SelectedLead.Name;
        LeadText.Visibility = Visibility.Visible;
        if (!string.IsNullOrEmpty(picker.SelectedLead.Email))
        {
            ToBox.Text = picker.SelectedLead.Email;
        }
    }

    private async void ComposeDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var to = ToBox.Text.Trim();
            if (!to.Contains('@') || !to.Contains('.'))
            {
                ErrorText.Text = "Enter a valid email address.";
                ErrorText.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(SubjectBox.Text) || string.IsNullOrWhiteSpace(BodyBox.Text))
            {
                ErrorText.Text = "Write a subject and a message first.";
                ErrorText.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }
            await _api.QueueEmailAsync(_leadId, to, SubjectBox.Text.Trim(), BodyBox.Text, inReplyTo: null);
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
}
