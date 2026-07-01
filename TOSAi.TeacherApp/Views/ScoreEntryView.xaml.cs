using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class ScoreEntryView : UserControl
{
    private readonly IScoreStore _scoreStore = new HttpScoreStore("https://tosai.onrender.com");
    private ObservableCollection<ScoreImportRow> _importRows = [];
    private bool _hasLoadedCloudData;

    public ScoreEntryView()
    {
        InitializeComponent();
        DataContext = new ScoreEntryViewData(BuildSummaryRows([]));
        Loaded += ScoreEntryView_Loaded;
    }

    private async void ScoreEntryView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoadedCloudData)
        {
            return;
        }

        _hasLoadedCloudData = true;
        await LoadCloudRowsAsync(showEmptyMessage: false);
    }

    private void ImportScoresButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "选择成绩 CSV 文件",
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _importRows = CsvScoreTemplateService.Import(dialog.FileName);
            ImportRowsDataGrid.ItemsSource = _importRows;
            RefreshSummaryRows();
            ImportStatusText.Text = $"已导入 {_importRows.Count} 条成绩明细：{Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "导入失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new()
        {
            Title = "导出成绩导入模板",
            Filter = "CSV 文件 (*.csv)|*.csv",
            FileName = "成绩导入模板.csv",
            AddExtension = true,
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            CsvScoreTemplateService.ExportTemplate(dialog.FileName);
            ImportStatusText.Text = $"模板已导出：{dialog.FileName}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "导出失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_importRows.Count == 0)
        {
            MessageBox.Show("还没有导入成绩明细。", "保存成绩", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            int importedCount = _importRows.Count;
            await _scoreStore.SaveAsync(_importRows);
            await LoadCloudRowsAsync(showEmptyMessage: false);
            ImportStatusText.Text = $"已合并保存 {importedCount} 条成绩明细到云端；当前云端共 {_importRows.Count} 条。";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        {
            MessageBox.Show(ex.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void LoadLocalButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadCloudRowsAsync(showEmptyMessage: true);
    }

    private async void ClearLocalButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show("确定要清空云端保存的成绩明细吗？", "清空云端数据", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _scoreStore.ClearAsync();
            _importRows.Clear();
            ImportRowsDataGrid.ItemsSource = _importRows;
            RefreshSummaryRows();
            ImportStatusText.Text = "云端成绩明细已清空。";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        {
            MessageBox.Show(ex.Message, "清空失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadCloudRowsAsync(bool showEmptyMessage)
    {
        try
        {
            _importRows = await _scoreStore.LoadAsync();
            ImportRowsDataGrid.ItemsSource = _importRows;
            RefreshSummaryRows();

            if (_importRows.Count > 0)
            {
                ImportStatusText.Text = $"已读取云端保存的 {_importRows.Count} 条成绩明细。";
            }
            else if (showEmptyMessage)
            {
                ImportStatusText.Text = "云端还没有保存成绩明细。";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or HttpRequestException or TaskCanceledException)
        {
            MessageBox.Show(ex.Message, "读取失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RefreshSummaryRows()
    {
        DataContext = new ScoreEntryViewData(BuildSummaryRows(_importRows));
    }

    private static ObservableCollection<ScoreRecord> BuildSummaryRows(IEnumerable<ScoreImportRow> rows)
    {
        List<ScoreRecord> summaries = rows
            .GroupBy(row => new { row.StudentId, row.StudentName, row.ClassName })
            .OrderBy(group => group.Key.ClassName)
            .ThenBy(group => group.Key.StudentName)
            .Select(group => new ScoreRecord
            {
                StudentName = group.Key.StudentName,
                ClassName = group.Key.ClassName,
                Chinese = PickSubjectScore(group, "语文"),
                Math = PickSubjectScore(group, "数学"),
                English = PickSubjectScore(group, "英语"),
                Physics = PickSubjectScore(group, "物理"),
                Chemistry = PickSubjectScore(group, "化学")
            })
            .ToList();

        return new ObservableCollection<ScoreRecord>(summaries);
    }

    private static double PickSubjectScore(IEnumerable<ScoreImportRow> rows, string subjectName)
    {
        ScoreImportRow? latestRow = rows
            .Where(row => string.Equals(row.SubjectName, subjectName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(row => row.ExamDate)
            .ThenByDescending(row => row.ExamName)
            .FirstOrDefault();

        return latestRow?.Score ?? 0;
    }
}

public sealed record ScoreEntryViewData(ObservableCollection<ScoreRecord> Scores);