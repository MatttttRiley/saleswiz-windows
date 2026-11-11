using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace SalesWiZ.Views;

public sealed partial class LoginPage : UserControl
{
    public event EventHandler? LoginSucceeded;

    private App ApiOwner => (App)Application.Current;

    public LoginPage()
    {
        InitializeComponent();
    }

    public void FocusPassword() => PasswordInput.Focus(FocusState.Programmatic);

    public void Reset()
    {
        PasswordInput.Password = "";
        ErrorText.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = true;
        BusyRing.IsActive = false;
        BusyRing.Visibility = Visibility.Collapsed;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await LogInAsync();
    }

    private async void PasswordInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await LogInAsync();
        }
    }

    private async Task LogInAsync()
    {
        if (string.IsNullOrEmpty(PasswordInput.Password)) return;
        LoginButton.IsEnabled = false;
        BusyRing.IsActive = true;
        BusyRing.Visibility = Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;

        var ok = await ApiOwner.Api.LoginAsync(PasswordInput.Password);

        BusyRing.IsActive = false;
        BusyRing.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = true;

        if (ok)
        {
            PasswordInput.Password = "";
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorText.Text = ApiOwner.Api.AuthError ?? "Wrong password.";
            ErrorText.Visibility = Visibility.Visible;
            PasswordInput.Focus(FocusState.Programmatic);
        }
    }
}
