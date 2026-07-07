using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class ReportCenterView : UserControl
{
    private readonly IReportDraftStore _draftStore = new HttpReportDraftStore(ApiEndpointOptions.BaseUrl);
    private ObservableCollection<ReportDraft> _drafts = [];

    public ReportCenterView()
    {
        InitializeComponent();
        Loaded += ReportCenterView_Loaded;
    }

    private async void ReportCenterView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDraftsAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDraftsAsync();
    }

    private void DraftsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ShowDraft(DraftsDataGrid.SelectedItem as ReportDraft);
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (DraftsDataGrid.SelectedItem is not ReportDraft draft)
        {
            MessageBox.Show("请先选择一个报告草稿。", "删除草稿", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBoxResult result = MessageBox.Show("确定要删除这个报告草稿吗？", "删除草稿", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _draftStore.DeleteAsync(draft.Id);
            await LoadDraftsAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "删除草稿失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (DraftsDataGrid.SelectedItem is not ReportDraft draft)
        {
            MessageBox.Show("请先选择一个报告草稿。", "导出草稿", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = "导出报告草稿",
            Filter = "文本文件 (*.txt)|*.txt",
            FileName = $"TOSAI报告草稿-{draft.CreatedAt.ToLocalTime():yyyyMMdd-HHmm}.txt",
            AddExtension = true,
            DefaultExt = ".txt"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, BuildExportText(draft), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        StatusText.Text = $"已导出：{dialog.FileName}";
    }

    private async Task LoadDraftsAsync()
    {
        try
        {
            _drafts = await _draftStore.LoadAsync();
            DraftsDataGrid.DataContext = _drafts;
            DraftsDataGrid.SelectedIndex = _drafts.Count > 0 ? 0 : -1;
            StatusText.Text = _drafts.Count == 0 ? "暂无报告草稿。" : $"已读取 {_drafts.Count} 份报告草稿。";
            if (_drafts.Count == 0)
            {
                ShowDraft(null);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _drafts = [];
            DraftsDataGrid.DataContext = _drafts;
            ShowDraft(null);
            StatusText.Text = "报告草稿读取失败。";
            MessageBox.Show(ex.Message, "读取报告草稿失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ShowDraft(ReportDraft? draft)
    {
        SummaryTextBox.Text = draft?.Summary ?? string.Empty;
        SuggestionsTextBox.Text = draft?.Suggestions ?? string.Empty;
        PromptTextBox.Text = draft?.Prompt ?? string.Empty;
    }

    private static string BuildExportText(ReportDraft draft)
    {
        StringBuilder builder = new();
        builder.AppendLine("TOS AI 报告草稿");
        builder.AppendLine($"生成时间：{draft.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}");
        builder.AppendLine($"分析范围：{draft.Scope}");
        builder.AppendLine();
        builder.AppendLine("摘要");
        builder.AppendLine(draft.Summary);
        builder.AppendLine();
        builder.AppendLine("教学建议");
        builder.AppendLine(draft.Suggestions);
        builder.AppendLine();
        builder.AppendLine("原始要求");
        builder.AppendLine(draft.Prompt);
        return builder.ToString();
    }
}