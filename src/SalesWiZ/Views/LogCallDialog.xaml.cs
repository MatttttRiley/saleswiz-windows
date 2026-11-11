using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SalesWiZ.Views;

public sealed partial class LogCallDialog : ContentDialog
{
    public static readonly string[] Outcomes =
    [
        "no answer", "hung up", "not interested", "interested", "voicemail", "callback requested",
    ];

    public string Outcome => OutcomeCombo.SelectedItem as string ?? "no answer";
    public string? Note => string.IsNullOrWhiteSpace(NoteBox.Text) ? null : NoteBox.Text.Trim();

    /// <summary>yyyy-MM-dd, or null when no follow-up date was set.</summary>
    public string? FollowUpDue =>
        FollowUpCheck.IsChecked == true ? FollowUpDate.Date.ToString("yyyy-MM-dd") : null;

    public LogCallDialog()
    {
        InitializeComponent();
        OutcomeCombo.ItemsSource = Outcomes;
        OutcomeCombo.SelectedIndex = 0;
        FollowUpDate.Date = DateTimeOffset.Now.AddDays(2);
    }

    private void FollowUpCheck_Toggled(object sender, RoutedEventArgs e)
    {
        FollowUpDate.IsEnabled = FollowUpCheck.IsChecked == true;
    }

    private void LogCallDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validation happens in the caller via the API; nothing to block here.
    }
}
