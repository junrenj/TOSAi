using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class StudentTrendView : UserControl
{
    private const string AllSubjects = "全部学科";

    private readonly IScoreStore _scoreStore = new HttpScoreStore(ApiEndpointOptions.BaseUrl);
    private ObservableCollection<ScoreImportRow> _allRows = [];
    private bool _isUpdatingFilters;

    public StudentTrendView()
    {
        InitializeComponent();
        Loaded += StudentTrendView_Loaded;
    }

    private async void StudentTrendView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadRowsAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadRowsAsync();
    }

    private void StudentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingFilters)
        {
            return;
        }

        PopulateSubjects();
        RefreshTrend();
    }

    private void SubjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingFilters)
        {
            return;
        }

        RefreshTrend();
    }

    private async Task LoadRowsAsync()
    {
        try
        {
            _allRows = await _scoreStore.LoadAsync();
            PopulateStudents();
            RefreshTrend();

            StatusText.Text = _allRows.Count == 0
                ? "暂无云端成绩数据"
                : $"已读取 {_allRows.Count} 条云端成绩明细";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _allRows = [];
            PopulateStudents();
            RefreshTrend();
            StatusText.Text = "云端成绩数据读取失败";
            MessageBox.Show(ex.Message, "读取云端成绩失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PopulateStudents()
    {
        _isUpdatingFilters = true;

        List<StudentOption> students = _allRows
            .GroupBy(row => new { row.StudentId, row.StudentName })
            .OrderBy(group => group.Key.StudentName)
            .Select(group => new StudentOption(group.Key.StudentId, group.Key.StudentName))
            .ToList();

        StudentComboBox.ItemsSource = students;
        StudentComboBox.SelectedIndex = students.Count > 0 ? 0 : -1;

        _isUpdatingFilters = false;
        PopulateSubjects();
    }

    private void PopulateSubjects()
    {
        _isUpdatingFilters = true;

        string? studentId = StudentComboBox.SelectedValue as string;
        List<string> subjects = [AllSubjects];

        if (!string.IsNullOrWhiteSpace(studentId))
        {
            subjects.AddRange(_allRows
                .Where(row => row.StudentId == studentId)
                .Select(row => row.SubjectName)
                .Distinct()
                .OrderBy(subject => subject));
        }

        SubjectComboBox.ItemsSource = subjects;
        SubjectComboBox.SelectedIndex = 0;

        _isUpdatingFilters = false;
    }

    private void RefreshTrend()
    {
        if (StudentComboBox.SelectedValue is not string studentId)
        {
            ClearSummary();
            TrendItemsControl.ItemsSource = null;
            TrendDataGrid.ItemsSource = null;
            return;
        }

        string? subject = SubjectComboBox.SelectedItem as string;
        List<TrendRow> trendRows = _allRows
            .Where(row => row.StudentId == studentId)
            .Where(row => subject is null || subject == AllSubjects || row.SubjectName == subject)
            .OrderBy(row => row.ExamDate)
            .ThenBy(row => row.SubjectName)
            .Select(row => new TrendRow(
                row.ExamName,
                row.ExamDate,
                row.ClassName,
                row.SubjectName,
                row.Score,
                row.FullScore,
                row.ScoreRate,
                $"{row.Score}/{row.FullScore}"))
            .ToList();

        TrendItemsControl.ItemsSource = trendRows;
        TrendDataGrid.ItemsSource = trendRows;
        RefreshSummary(studentId);
    }

    private void RefreshSummary(string studentId)
    {
        List<ScoreImportRow> studentRows = _allRows.Where(row => row.StudentId == studentId).ToList();
        if (studentRows.Count == 0)
        {
            ClearSummary();
            return;
        }

        int examCount = studentRows
            .Select(row => new { row.ExamName, row.ExamDate })
            .Distinct()
            .Count();

        double averageRate = System.Math.Round(studentRows.Average(row => row.ScoreRate), 1);

        var subjectGroups = studentRows
            .GroupBy(row => row.SubjectName)
            .Select(group => new
            {
                Subject = group.Key,
                AverageRate = System.Math.Round(group.Average(row => row.ScoreRate), 1)
            })
            .OrderByDescending(item => item.AverageRate)
            .ToList();

        ExamCountText.Text = examCount.ToString();
        AverageRateText.Text = $"{averageRate}%";
        BestSubjectText.Text = subjectGroups.FirstOrDefault() is { } best ? $"{best.Subject}（{best.AverageRate}%）" : "-";
        WeakSubjectText.Text = subjectGroups.LastOrDefault() is { } weak ? $"{weak.Subject}（{weak.AverageRate}%）" : "-";
    }

    private void ClearSummary()
    {
        ExamCountText.Text = "-";
        AverageRateText.Text = "-";
        BestSubjectText.Text = "-";
        WeakSubjectText.Text = "-";
    }

    private sealed record StudentOption(string StudentId, string StudentName)
    {
        public string DisplayName => $"{StudentName}（{StudentId}）";
    }

    private sealed record TrendRow(
        string ExamName,
        DateOnly ExamDate,
        string ClassName,
        string SubjectName,
        double Score,
        double FullScore,
        double ScoreRate,
        string ScoreText);
}
