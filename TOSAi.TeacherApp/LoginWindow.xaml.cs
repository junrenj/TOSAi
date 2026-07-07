using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp;

public partial class LoginWindow : Window
{
    private readonly HttpAuthClient _authClient = new(ApiEndpointOptions.BaseUrl);
    private bool _isLoggingIn;

    public LoginWindow()
    {
        InitializeComponent();
        RoleComboBox.SelectedIndex = 0;
    }

    private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RoleComboBox.SelectedItem is not ComboBoxItem { Tag: string role })
        {
            return;
        }

        UsernameTextBox.Text = role.ToLowerInvariant();
        PasswordBox.Password = role.ToLowerInvariant() + "123";
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        if (_isLoggingIn)
        {
            return;
        }

        if (RoleComboBox.SelectedItem is not ComboBoxItem { Tag: string role })
        {
            return;
        }

        _isLoggingIn = true;
        LoginButton.IsEnabled = false;
        RoleComboBox.IsEnabled = false;
        UsernameTextBox.IsEnabled = false;
        PasswordBox.IsEnabled = false;
        StatusText.Text = "正在登录...";

        try
        {
            LoginResponse response = await _authClient.LoginAsync(role, UsernameTextBox.Text.Trim(), PasswordBox.Password.Trim());
            AuthSession.SignIn(response.Token, response.User);
            DialogResult = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Text.Json.JsonException)
        {
            StatusText.Text = ex.Message;
            _isLoggingIn = false;
            LoginButton.IsEnabled = true;
            RoleComboBox.IsEnabled = true;
            UsernameTextBox.IsEnabled = true;
            PasswordBox.IsEnabled = true;
        }
    }
}