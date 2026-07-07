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

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await LoginAsync();
        }
    }

    private async Task LoginAsync()
    {
        if (RoleComboBox.SelectedItem is not ComboBoxItem { Tag: string role })
        {
            return;
        }

        LoginButton.IsEnabled = false;
        StatusText.Text = "正在登录...";

        try
        {
            LoginResponse response = await _authClient.LoginAsync(role, UsernameTextBox.Text.Trim(), PasswordBox.Password.Trim());
            AuthSession.SignIn(response.Token, response.User);
            DialogResult = true;
            Close();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Text.Json.JsonException)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}