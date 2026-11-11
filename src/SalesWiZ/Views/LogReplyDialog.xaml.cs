using Microsoft.UI.Xaml.Controls;

namespace SalesWiZ.Views;

public sealed partial class LogReplyDialog : ContentDialog
{
    public string? Note => string.IsNullOrWhiteSpace(NoteBox.Text) ? null : NoteBox.Text.Trim();

    public LogReplyDialog()
    {
        InitializeComponent();
    }
}
