using System.Windows.Controls;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class PlatformFeatureView : UserControl
{
    private readonly IPlatformApiClient _apiClient;
    private readonly UserRole _role;
    private readonly string _pageKey;

    public PlatformFeatureView(IPlatformApiClient apiClient, UserRole role, string pageKey)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _role = role;
        _pageKey = pageKey;
        Loaded += PlatformFeatureView_Loaded;
    }

    private async void PlatformFeatureView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        PlatformFeaturePage page = await _apiClient.GetFeaturePageAsync(_role, _pageKey);
        CardsItemsControl.ItemsSource = page.Cards;
        ActivitiesItemsControl.ItemsSource = page.Activities;
        NoteTextBlock.Text = page.Note;
    }
}
