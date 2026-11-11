using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SalesWiZ.Views;

/// <summary>
/// Reply to an inbound email. Queues it via queue_email (in_reply_to = the
/// original's Gmail id) — it only sends after Matt approves the send.
/// Subject is "Re: …" and the original is appended quoted, same as the web
/// inbox and the iPhone app.
/// </summary>
public sealed partial class ReplyDialog : ContentDialog
{
    private readonly ApiClient _api;
    private readonly EmailMessage _original;

    /// <summary>The original, quoted, appended to the sent body.</summary>
    public string QuotedOriginal { get; }

    public ReplyDialog(ApiClient api, EmailMessage original)
    {
        _api = api;
        _original = original;
        InitializeComponent();

        ToBox.Text = original.FromAddr ?? "";
        SubjectBox.Text = ReplySubject(original.Subject);
        QuotedOriginal = BuildQuotedOriginal(original);
        QuotedText.Text = QuotedOriginal;
    }

    private static string ReplySubject(string? subject)
    {
        if (string.IsNullOrEmpty(subject)) return "Re: ";
        return subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
            ? subject : $"Re: {subject}";
    }

    private static string BuildQuotedOriginal(EmailMessage original)
    {
        var body = original.Body ?? "";
        var quoted = string.Join("\n", body.Split('\n').Select(line => $"> {line}"));
        var date = Helpers.PrettyDateTime(original.ReceivedAt);
        return $"\n\nOn {date}, {original.FromAddr ?? ""} wrote:\n{quoted}";
    }

    private async void ReplyDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            if (string.IsNullOrWhiteSpace(SubjectBox.Text) || string.IsNullOrWhiteSpace(BodyBox.Text))
            {
                ErrorText.Text = "Write a subject and a message first.";
                ErrorText.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }
            await _api.QueueEmailAsync(
                _original.LeadId,
                ToBox.Text.Trim(),
                SubjectBox.Text.Trim(),
                BodyBox.Text + QuotedOriginal,
                _original.GmailId);
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
