using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class ScoreEntryView : UserControl
{
    private readonly LocalScoreStore _scoreStore = new();
    private ObservableCollection<ScoreImportRow> _importRows = [];
    private bool _hasLoadedLocalData;

    public ScoreEntryView()
    {
        InitializeComponent();
        DataContext = new ScoreEntryViewData(SampleDataService.GetScores());
        Loaded += ScoreEntryView_Loaded;
    }

    private async void ScoreEntryView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoadedLocalData)
        {
            return;
        }

        _hasLoadedLocalData = true;
        await LoadLocalRowsAsync(showEmptyMessage: false);
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
            await _scoreStore.SaveAsync(_importRows);
            ImportStatusText.Text = $"已保存 {_importRows.Count} 条成绩明细到本地：{_scoreStore.DataFilePath}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void LoadLocalButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadLocalRowsAsync(showEmptyMessage: true);
    }

    private async void ClearLocalButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show("确定要清空本地保存的成绩明细吗？", "清空本地数据", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _scoreStore.ClearAsync();
            _importRows.Clear();
            ImportRowsDataGrid.ItemsSource = _importRows;
            ImportStatusText.Text = "本地成绩明细已清空。";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "清空失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadLocalRowsAsync(bool showEmptyMessage)
    {
        try
        {
            _importRows = await _scoreStore.LoadAsync();
            ImportRowsDataGrid.ItemsSource = _importRows;

            if (_importRows.Count > 0)
            {
                ImportStatusText.Text = $"已读取本地保存的 {_importRows.Count} 条成绩明细。";
            }
            else if (showEmptyMessage)
            {
                ImportStatusText.Text = "本地还没有保存成绩明细。";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            MessageBox.Show(ex.Message, "读取失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

public sealed record ScoreEntryViewData(ObservableCollection<ScoreRecord> Scores);
