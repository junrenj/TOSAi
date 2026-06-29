using System.IO;
using System.Windows;
using System.Windows.Controls;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class SettingsView : UserControl
{
    private readonly AiSettingsStore _settingsStore = new();
    private readonly LocalScoreStore _scoreStore = new();

    public SettingsView()
    {
        InitializeComponent();
        Loaded += SettingsView_Loaded;
    }

    private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
        ScoreDataPathTextBox.Text = _scoreStore.DataFilePath;
        AiSettingsPathTextBox.Text = _settingsStore.SettingsFilePath;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AiSettings settings = new()
            {
                Provider = GetSelectedProvider(),
                Endpoint = EndpointTextBox.Text.Trim(),
                Model = ModelTextBox.Text.Trim(),
                ApiKey = ApiKeyPasswordBox.Password.Trim()
            };

            await _settingsStore.SaveAsync(settings);
            StatusText.Text = $"AI 设置已保存：{Path.GetFileName(_settingsStore.SettingsFilePath)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "保存设置失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            AiSettings settings = await _settingsStore.LoadAsync();
            SelectProvider(settings.Provider);
            EndpointTextBox.Text = settings.Endpoint;
            ModelTextBox.Text = settings.Model;
            ApiKeyPasswordBox.Password = settings.ApiKey;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            MessageBox.Show(ex.Message, "读取设置失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string GetSelectedProvider()
    {
        return (ProviderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "暂不接入（模拟分析）";
    }

    private void SelectProvider(string provider)
    {
        foreach (ComboBoxItem item in ProviderComboBox.Items)
        {
            if (string.Equals(item.Content?.ToString(), provider, StringComparison.Ordinal))
            {
                ProviderComboBox.SelectedItem = item;
                return;
            }
        }

        ProviderComboBox.SelectedIndex = 0;
    }
}
