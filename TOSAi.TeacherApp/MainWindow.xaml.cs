using System.Windows;
using System.Windows.Controls;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;
using TOSAi.TeacherApp.Views;

namespace TOSAi.TeacherApp;

public partial class MainWindow : Window
{
    private readonly IPlatformApiClient _apiClient = new FallbackPlatformApiClient(
        new HttpPlatformApiClient("https://tosai.onrender.com"),
        new MockPlatformApiClient());
    private readonly Dictionary<string, PageRegistration> _pages = new();
    private readonly Dictionary<UserRole, IReadOnlyList<NavigationRegistration>> _navigation = new();
    private UserRole _currentRole = UserRole.Teacher;
    private string _currentPageKey = "teacherOverview";

    public MainWindow()
    {
        InitializeComponent();
        RegisterPages();
        RegisterNavigation();
        RoleComboBox.SelectedIndex = 0;
        SwitchRole(UserRole.Teacher);
    }

    private void RegisterPages()
    {
        _pages["teacherOverview"] = new("教师端总览", "查看年级整体进展、学科表现和重点学生状态。", () => new DashboardView());
        _pages["teacherStudents"] = new("学生档案", "查看学生分布、名单概览和关注学生情况。", () => new StudentListView());
        _pages["teacherScores"] = new("成绩录入", "通过 CSV 模板导入扫描或考试成绩，并保存到云端数据层。", () => new ScoreEntryView());
        _pages["teacherTrends"] = new("学生趋势", "读取云端成绩明细，按学生和学科查看历次成绩变化。", () => new StudentTrendView());
        _pages["teacherAssignmentGenerator"] = new("生成作业", "按主题、能力方向、情景分类和难度生成作业。", () => new AssignmentGeneratorView());
        _pages["teacherAssignments"] = new("作业管理", "按学生和日期查看作业完成情况和学习记录。", () => new AssignmentManagementView());
        _pages["teacherReports"] = new("报告中心", "管理周报、家长报告、风险提醒和阅读反馈。", () => new PlatformFeatureView(_apiClient, UserRole.Teacher, "teacherReports"));

        _pages["studentHome"] = new("学生首页", "查看今日任务、学习计划和当前学习状态。", () => new PlatformFeatureView(_apiClient, UserRole.Student, "studentHome"));
        _pages["studentHomework"] = new("我的作业", "查看待完成、已提交和待订正作业。", () => new PlatformFeatureView(_apiClient, UserRole.Student, "studentHomework"));
        _pages["studentProgress"] = new("学习进度", "查看自己的学习计划完成度和学科变化。", () => new PlatformFeatureView(_apiClient, UserRole.Student, "studentProgress"));
        _pages["studentPhotoQuestion"] = new("拍照搜题", "预留拍照搜题、题目讲解和错题沉淀入口。", () => new PlatformFeatureView(_apiClient, UserRole.Student, "studentPhotoQuestion"));

        _pages["parentHome"] = new("家长首页", "查看孩子近期成绩、作业完成、优势学科和关注点。", () => new PlatformFeatureView(_apiClient, UserRole.Parent, "parentHome"));
        _pages["parentTrends"] = new("成绩趋势", "查看孩子排名变化、考试趋势和学科表现。", () => new PlatformFeatureView(_apiClient, UserRole.Parent, "parentTrends"));
        _pages["parentReports"] = new("学习报告", "查看周报、月报、教师反馈和家庭建议。", () => new PlatformFeatureView(_apiClient, UserRole.Parent, "parentReports"));
        _pages["parentWellbeing"] = new("心理关注", "查看学习压力、情绪状态和家庭沟通建议。", () => new PlatformFeatureView(_apiClient, UserRole.Parent, "parentWellbeing"));

        _pages["settings"] = new("系统设置", "配置本地数据路径、服务器接口和 AI 服务参数。", () => new SettingsView());
    }

    private void RegisterNavigation()
    {
        _navigation[UserRole.Teacher] =
        [
            new("teacherOverview", "教师总览"),
            new("teacherStudents", "学生档案"),
            new("teacherScores", "成绩录入"),
            new("teacherTrends", "学生趋势"),
            new("teacherAssignmentGenerator", "生成作业"),
            new("teacherAssignments", "作业管理"),
            new("teacherReports", "报告中心")
        ];

        _navigation[UserRole.Student] =
        [
            new("studentHome", "学生首页"),
            new("studentHomework", "我的作业"),
            new("studentProgress", "学习进度"),
            new("studentPhotoQuestion", "拍照搜题")
        ];

        _navigation[UserRole.Parent] =
        [
            new("parentHome", "家长首页"),
            new("parentTrends", "成绩趋势"),
            new("parentReports", "学习报告"),
            new("parentWellbeing", "心理关注")
        ];
    }

    private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RoleComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string roleTag)
        {
            return;
        }

        if (Enum.TryParse(roleTag, out UserRole role))
        {
            SwitchRole(role);
        }
    }

    private void SwitchRole(UserRole role)
    {
        _currentRole = role;
        NavigationPanel.Children.Clear();

        foreach (NavigationRegistration item in _navigation[role])
        {
            RadioButton button = new()
            {
                Content = item.Label,
                Tag = item.PageKey,
                GroupName = "MainNavigation",
                Style = (Style)FindResource("NavRadioButton")
            };
            button.Checked += NavigationButton_Checked;
            NavigationPanel.Children.Add(button);
        }

        if (NavigationPanel.Children.OfType<RadioButton>().FirstOrDefault() is { } firstButton)
        {
            firstButton.IsChecked = true;
        }
    }

    private void NavigationButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string pageKey })
        {
            ShowPage(pageKey);
        }
    }

    private void ShowPage(string pageKey)
    {
        if (!_pages.TryGetValue(pageKey, out PageRegistration? page))
        {
            return;
        }

        _currentPageKey = pageKey;
        PageTitleText.Text = page.Title;
        PageSubtitleText.Text = page.Subtitle;
        MainContentHost.Content = page.CreateView();
    }

    private void RefreshPageButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(_currentPageKey);
    }

    private sealed record PageRegistration(string Title, string Subtitle, Func<UserControl> CreateView);

    private sealed record NavigationRegistration(string PageKey, string Label);
}

