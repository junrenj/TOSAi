using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class AnalysisView : UserControl
{
    private readonly AiSettingsStore _settingsStore = new();
    private readonly IScoreStore _scoreStore = new HttpScoreStore(ApiEndpointOptions.BaseUrl);
    private readonly IReportDraftStore _reportDraftStore = new HttpReportDraftStore(ApiEndpointOptions.BaseUrl);
    private readonly IAiAnalysisService _mockAnalysisService = new MockAiAnalysisService();
    private ReportDraft? _pendingReportDraft;

    public AnalysisView()
    {
        InitializeComponent();
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        GenerateButton.IsEnabled = false;
        SaveReportButton.IsEnabled = false;
        _pendingReportDraft = null;
        SummaryTextBox.Text = "正在生成分析...";
        SuggestionsTextBox.Text = string.Empty;

        try
        {
            string scope = (ScopeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "全年级";
            AiSettings settings = await _settingsStore.LoadAsync();
            IReadOnlyList<ScoreImportRow> rows = await _scoreStore.LoadAsync();
            string prompt = BuildPrompt(PromptTextBox.Text, rows);

            IAiAnalysisService analysisService = settings.UseMockAnalysis
                ? _mockAnalysisService
                : new OpenAiCompatibleAnalysisService(settings);

            ServiceStatusText.Text = settings.UseMockAnalysis
                ? "当前使用模拟分析。请在系统设置中填写接口地址、模型名称和 API Key 后启用真实 AI。"
                : $"正在调用真实 AI 接口：{settings.Model}";

            AiAnalysisResult result = await analysisService.AnalyzeAsync(new AiAnalysisRequest(scope, prompt));
            SummaryTextBox.Text = result.Summary;
            SuggestionsTextBox.Text = result.Suggestions;
            _pendingReportDraft = new ReportDraft
            {
                Scope = scope,
                Prompt = PromptTextBox.Text.Trim(),
                Summary = result.Summary,
                Suggestions = result.Suggestions
            };
            SaveReportButton.IsEnabled = true;
            ServiceStatusText.Text = settings.UseMockAnalysis ? "模拟分析完成，可保存为报告草稿。" : "真实 AI 分析完成，可保存为报告草稿。";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            SummaryTextBox.Text = "分析失败。";
            SuggestionsTextBox.Text = ex.Message;
            ServiceStatusText.Text = "请检查系统设置中的 AI 接口配置，或确认网络和 API Key 是否可用。";
        }
        finally
        {
            GenerateButton.IsEnabled = true;
        }
    }

    private async void SaveReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingReportDraft is null)
        {
            MessageBox.Show("请先生成分析报告。", "保存报告草稿", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SaveReportButton.IsEnabled = false;
        try
        {
            ReportDraft saved = await _reportDraftStore.SaveAsync(_pendingReportDraft);
            _pendingReportDraft = saved;
            ServiceStatusText.Text = $"报告草稿已保存：{saved.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            MessageBox.Show(ex.Message, "保存报告草稿失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            SaveReportButton.IsEnabled = true;
        }
    }
    private static string BuildPrompt(string userPrompt, IReadOnlyList<ScoreImportRow> rows)
    {
        StringBuilder builder = new();
        builder.AppendLine("教师要求：");
        builder.AppendLine(userPrompt.Trim());
        builder.AppendLine();
        builder.AppendLine("请按以下结构输出：");
        builder.AppendLine("1. 总体判断");
        builder.AppendLine("2. 优势学科");
        builder.AppendLine("3. 薄弱学科");
        builder.AppendLine("4. 需要重点关注的学生类型");
        builder.AppendLine("5. 分层教学建议");
        builder.AppendLine();

        if (rows.Count == 0)
        {
            builder.AppendLine("当前云端没有保存成绩明细，请基于教师要求给出通用分析框架。");
            return builder.ToString();
        }

        builder.AppendLine("云端成绩明细，格式：考试名称 | 日期 | 年级 | 班级 | 学号 | 姓名 | 学科 | 分数/满分 | 得分率");

        foreach (ScoreImportRow row in rows.Take(200))
        {
            builder.AppendLine($"{row.ExamName} | {row.ExamDate} | {row.GradeName} | {row.ClassName} | {row.StudentId} | {row.StudentName} | {row.SubjectName} | {row.Score}/{row.FullScore} | {row.ScoreRate}%");
        }

        if (rows.Count > 200)
        {
            builder.AppendLine($"其余 {rows.Count - 200} 条明细未展开，请基于已展示样本和统计需求给出建议。");
        }

        return builder.ToString();
    }
}

