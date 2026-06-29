using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace TOSAi.TeacherApp.Views;

public partial class AssignmentManagementView : UserControl
{
    private readonly List<StudentAssignmentProfile> _profiles;
    private bool _isLoadingFilters;
    private AssignmentAttempt? _currentAttempt;
    private StudentAssignmentProfile? _currentProfile;

    public AssignmentManagementView()
    {
        InitializeComponent();
        _profiles = CreateProfiles();
        InitializeFilters();
    }

    private void InitializeFilters()
    {
        _isLoadingFilters = true;
        StudentComboBox.ItemsSource = _profiles.Select(profile => new StudentSelector(profile.StudentId, profile.StudentName, profile.ClassName)).ToList();
        StudentComboBox.SelectedIndex = 0;
        PopulateDates();
        _isLoadingFilters = false;
        RefreshView();
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFilters)
        {
            return;
        }

        if (sender == StudentComboBox)
        {
            PopulateDates();
        }

        RefreshView();
    }

    private void PopulateDates()
    {
        _isLoadingFilters = true;

        string? studentId = StudentComboBox.SelectedValue as string;
        StudentAssignmentProfile? profile = _profiles.FirstOrDefault(item => item.StudentId == studentId);
        AssignmentDateComboBox.ItemsSource = profile?.Attempts.Select(attempt => attempt.Date.ToString("yyyy-MM-dd")).ToList() ?? [];
        AssignmentDateComboBox.SelectedIndex = AssignmentDateComboBox.Items.Count > 0 ? 0 : -1;

        _isLoadingFilters = false;
    }

    private void RefreshView()
    {
        string? studentId = StudentComboBox.SelectedValue as string;
        string? dateText = AssignmentDateComboBox.SelectedItem as string;

        _currentProfile = _profiles.FirstOrDefault(profile => profile.StudentId == studentId);
        _currentAttempt = _currentProfile?.Attempts.FirstOrDefault(attempt => attempt.Date.ToString("yyyy-MM-dd") == dateText);

        if (_currentProfile is null || _currentAttempt is null)
        {
            ClearView();
            return;
        }

        SubmitStatusText.Text = _currentAttempt.SubmitStatus;
        DurationText.Text = $"{_currentAttempt.DurationMinutes} 分钟";
        AccuracyText.Text = $"{_currentAttempt.AccuracyRate}%";
        WeakModuleText.Text = _currentAttempt.Modules.OrderBy(module => module.MasteryRate).FirstOrDefault()?.ModuleName ?? "-";
        ModuleItemsControl.ItemsSource = _currentAttempt.Modules;
        QuestionDataGrid.ItemsSource = _currentAttempt.Questions;
        HistoryItemsControl.ItemsSource = _currentProfile.History;

        DetailTitleText.Text = $"{_currentProfile.StudentName} - {_currentAttempt.AssignmentTitle}";
        DetailSubtitleText.Text = $"{_currentProfile.ClassName}，{_currentAttempt.Date:yyyy-MM-dd}，{_currentAttempt.TopicSummary}";
        ProfileSummaryText.Text = BuildProfileSummary(_currentProfile);
        StatusText.Text = $"已读取 {_currentProfile.StudentName} 在 {_currentAttempt.Date:yyyy-MM-dd} 的作业表现。";
    }

    private void ClearView()
    {
        SubmitStatusText.Text = "-";
        DurationText.Text = "-";
        AccuracyText.Text = "-";
        WeakModuleText.Text = "-";
        ModuleItemsControl.ItemsSource = null;
        QuestionDataGrid.ItemsSource = null;
        HistoryItemsControl.ItemsSource = null;
        ProfileSummaryText.Text = string.Empty;
        StatusText.Text = "暂无可展示数据。";
    }

    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile is null)
        {
            MessageBox.Show("请先选择学生。", "历史回顾", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(BuildProfileSummary(_currentProfile), $"{_currentProfile.StudentName} 的学生档案", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DownloadCurrentReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile is null || _currentAttempt is null)
        {
            MessageBox.Show("请先选择学生和作业日期。", "下载报告", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string fileName = $"{_currentProfile.StudentName}-{_currentAttempt.Date:yyyyMMdd}-作业报告.html";
        SaveHtml(fileName, BuildCurrentAttemptHtml(_currentProfile, _currentAttempt));
    }

    private void DownloadProfileReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile is null)
        {
            MessageBox.Show("请先选择学生。", "下载档案", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string fileName = $"{_currentProfile.StudentName}-学习档案.html";
        SaveHtml(fileName, BuildProfileHtml(_currentProfile));
    }

    private void SaveHtml(string fileName, string html)
    {
        SaveFileDialog dialog = new()
        {
            Title = "下载 HTML 报告",
            Filter = "HTML 文件 (*.html)|*.html",
            FileName = fileName,
            AddExtension = true,
            DefaultExt = ".html"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            StatusText.Text = $"报告已下载：{dialog.FileName}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "下载失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string BuildProfileSummary(StudentAssignmentProfile profile)
    {
        IReadOnlyList<ModuleMastery> latestModules = profile.Attempts.OrderByDescending(attempt => attempt.Date).First().Modules;
        ModuleMastery best = latestModules.OrderByDescending(module => module.MasteryRate).First();
        ModuleMastery weak = latestModules.OrderBy(module => module.MasteryRate).First();
        double average = Math.Round(profile.History.Average(item => item.ScoreRate), 1);

        return $"{profile.StudentName} 近阶段考试和作业平均表现为 {average}%。总体优势模块是“{best.ModuleName}”，当前需要重点跟进“{weak.ModuleName}”。建议结合错题类型安排同主题、低跨度的递进练习。";
    }

    private static string BuildCurrentAttemptHtml(StudentAssignmentProfile profile, AssignmentAttempt attempt)
    {
        StringBuilder builder = BeginHtml($"{profile.StudentName} 作业报告");
        builder.AppendLine($"<h1>{EscapeHtml(profile.StudentName)} 作业报告</h1>");
        builder.AppendLine($"<p class=\"meta\">{EscapeHtml(profile.ClassName)} | {attempt.Date:yyyy-MM-dd} | {EscapeHtml(attempt.AssignmentTitle)}</p>");
        builder.AppendLine("<h2>完成概况</h2>");
        builder.AppendLine($"<p>提交状态：{EscapeHtml(attempt.SubmitStatus)}；完成用时：{attempt.DurationMinutes} 分钟；正确率：{attempt.AccuracyRate}%。</p>");
        AppendModuleTable(builder, attempt.Modules);
        builder.AppendLine("<h2>题目明细</h2>");
        builder.AppendLine("<table><tr><th>题号</th><th>题型</th><th>主题</th><th>情景</th><th>难度</th><th>结果</th><th>反馈</th></tr>");
        foreach (AssignmentQuestionResult question in attempt.Questions)
        {
            builder.AppendLine($"<tr><td>{question.Number}</td><td>{EscapeHtml(question.Type)}</td><td>{EscapeHtml(question.Topic)}</td><td>{EscapeHtml(question.Scenario)}</td><td>{EscapeHtml(question.Difficulty)}</td><td>{EscapeHtml(question.ResultText)}</td><td>{EscapeHtml(question.Feedback)}</td></tr>");
        }
        builder.AppendLine("</table>");
        EndHtml(builder);
        return builder.ToString();
    }

    private static string BuildProfileHtml(StudentAssignmentProfile profile)
    {
        StringBuilder builder = BeginHtml($"{profile.StudentName} 学习档案");
        builder.AppendLine($"<h1>{EscapeHtml(profile.StudentName)} 学习档案</h1>");
        builder.AppendLine($"<p class=\"meta\">{EscapeHtml(profile.ClassName)} | 学号 {EscapeHtml(profile.StudentId)}</p>");
        builder.AppendLine($"<p>{EscapeHtml(BuildProfileSummary(profile))}</p>");
        builder.AppendLine("<h2>考试与作业记录</h2>");
        builder.AppendLine("<table><tr><th>日期</th><th>类型</th><th>名称</th><th>表现</th></tr>");
        foreach (LearningHistoryItem item in profile.History)
        {
            builder.AppendLine($"<tr><td>{item.DateText}</td><td>{EscapeHtml(item.Category)}</td><td>{EscapeHtml(item.Title)}</td><td>{item.RateText}</td></tr>");
        }
        builder.AppendLine("</table>");
        builder.AppendLine("<h2>总体知识模块掌握情况</h2>");
        AppendModuleTable(builder, profile.Attempts.OrderByDescending(attempt => attempt.Date).First().Modules);
        EndHtml(builder);
        return builder.ToString();
    }

    private static StringBuilder BeginHtml(string title)
    {
        StringBuilder builder = new();
        builder.AppendLine("<!doctype html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\">");
        builder.AppendLine($"<title>{EscapeHtml(title)}</title>");
        builder.AppendLine("<style>body{font-family:'Microsoft YaHei',Arial,sans-serif;margin:40px;color:#111827;line-height:1.7}h1{text-align:center}.meta{text-align:center;color:#6b7280}table{border-collapse:collapse;width:100%;margin:14px 0 28px}th,td{border:1px solid #d1d5db;padding:8px;text-align:left}th{background:#eff6ff}.bar{height:10px;background:#e5e7eb;border-radius:999px}.fill{height:10px;background:#2563eb;border-radius:999px}</style>");
        builder.AppendLine("</head><body>");
        return builder;
    }

    private static void EndHtml(StringBuilder builder)
    {
        builder.AppendLine("</body></html>");
    }

    private static void AppendModuleTable(StringBuilder builder, IReadOnlyList<ModuleMastery> modules)
    {
        builder.AppendLine("<h2>知识模块掌握</h2>");
        builder.AppendLine("<table><tr><th>模块</th><th>掌握度</th><th>状态</th></tr>");
        foreach (ModuleMastery module in modules)
        {
            builder.AppendLine($"<tr><td>{EscapeHtml(module.ModuleName)}</td><td><div class=\"bar\"><div class=\"fill\" style=\"width:{module.MasteryRate}%\"></div></div>{module.MasteryRate}%</td><td>{EscapeHtml(module.StatusText)}</td></tr>");
        }
        builder.AppendLine("</table>");
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    private static List<StudentAssignmentProfile> CreateProfiles()
    {
        return
        [
            CreateProfile("20260101", "李明", "初二 1 班", 88, 74),
            CreateProfile("20260102", "王雨", "初二 1 班", 82, 68),
            CreateProfile("20260103", "陈然", "初二 2 班", 76, 61)
        ];
    }

    private static StudentAssignmentProfile CreateProfile(string studentId, string studentName, string className, int firstRate, int secondRate)
    {
        ObservableCollection<AssignmentAttempt> attempts =
        [
            CreateAttempt(new DateOnly(2026, 6, 24), "遗传与进化专题作业 A", firstRate),
            CreateAttempt(new DateOnly(2026, 6, 18), "基因表达与家系分析练习", secondRate)
        ];

        ObservableCollection<LearningHistoryItem> history =
        [
            new("2026-06-24", "作业", "遗传与进化专题作业 A", firstRate),
            new("2026-06-18", "作业", "基因表达与家系分析练习", secondRate),
            new("2026-06-10", "考试", "六月阶段测", Math.Min(96, firstRate + 3)),
            new("2026-05-28", "作业", "自然选择案例练习", Math.Max(50, secondRate - 4)),
            new("2026-05-16", "考试", "五月单元测", Math.Max(50, firstRate - 7))
        ];

        return new StudentAssignmentProfile(studentId, studentName, className, attempts, history);
    }

    private static AssignmentAttempt CreateAttempt(DateOnly date, string title, int accuracyRate)
    {
        ObservableCollection<ModuleMastery> modules =
        [
            new("孟德尔遗传", Clamp(accuracyRate + 6)),
            new("基因表达", Clamp(accuracyRate - 4)),
            new("自然选择", Clamp(accuracyRate + 1)),
            new("物种形成", Clamp(accuracyRate - 10)),
            new("模型与建模", Clamp(accuracyRate - 7))
        ];

        ObservableCollection<AssignmentQuestionResult> questions =
        [
            new(1, "选择题", "孟德尔遗传", "家系分析", "基础理解", accuracyRate >= 70, 3, "能识别显隐性关系，但条件表达还需更严谨。"),
            new(2, "选择题", "基因表达", "基因编辑伦理问题", "应用推理", accuracyRate >= 80, 4, "对中心法则迁移较好，伦理判断需引用材料证据。"),
            new(3, "选择题", "自然选择", "抗生素耐药性", "应用推理", accuracyRate >= 65, 5, "能解释选择压力，但容易把个体适应写成群体进化。"),
            new(4, "大题", "物种形成", "物种进化案例", "综合分析", accuracyRate >= 78, 12, "隔离机制分析不够完整，建议补充地理隔离到生殖隔离的链条。"),
            new(5, "大题", "孟德尔遗传", "遗传病调查", "综合分析", accuracyRate >= 72, 15, "概率计算基本正确，家系符号和推理步骤需要规范。")
        ];

        string status = accuracyRate >= 85 ? "已提交，表现稳定" : accuracyRate >= 70 ? "已提交，需订正" : "已提交，建议面批";
        return new AssignmentAttempt(date, title, status, 42 + (100 - accuracyRate) / 2, accuracyRate, "遗传、进化、模型推理综合练习", modules, questions);
    }

    private static int Clamp(int value) => Math.Clamp(value, 0, 100);
}

public sealed record StudentSelector(string StudentId, string StudentName, string ClassName)
{
    public string DisplayName => $"{StudentName}（{ClassName}）";
}

public sealed record StudentAssignmentProfile(
    string StudentId,
    string StudentName,
    string ClassName,
    ObservableCollection<AssignmentAttempt> Attempts,
    ObservableCollection<LearningHistoryItem> History);

public sealed record AssignmentAttempt(
    DateOnly Date,
    string AssignmentTitle,
    string SubmitStatus,
    int DurationMinutes,
    int AccuracyRate,
    string TopicSummary,
    ObservableCollection<ModuleMastery> Modules,
    ObservableCollection<AssignmentQuestionResult> Questions);

public sealed record ModuleMastery(string ModuleName, int MasteryRate)
{
    public string RateText => $"{MasteryRate}%";

    public string Icon => MasteryRate >= 85 ? "✓" : MasteryRate >= 70 ? "!" : "×";

    public string StatusText => MasteryRate >= 85 ? "掌握稳定" : MasteryRate >= 70 ? "基本掌握" : "需要跟进";

    public Brush StatusBrush => MasteryRate >= 85
        ? Brushes.SeaGreen
        : MasteryRate >= 70
            ? Brushes.DarkOrange
            : Brushes.IndianRed;
}

public sealed record AssignmentQuestionResult(
    int Number,
    string Type,
    string Topic,
    string Scenario,
    string Difficulty,
    bool IsCorrect,
    int TimeSpentMinutes,
    string Feedback)
{
    public string ResultText => IsCorrect ? "正确" : "错误";

    public string TimeSpentText => $"{TimeSpentMinutes} 分";
}

public sealed record LearningHistoryItem(string DateText, string Category, string Title, int ScoreRate)
{
    public string RateText => $"{ScoreRate}%";
}
