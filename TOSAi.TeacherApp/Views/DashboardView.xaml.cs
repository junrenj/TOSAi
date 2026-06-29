using System.Collections.ObjectModel;
using System.Windows.Controls;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContext = new DashboardViewData(
            new ObservableCollection<DashboardMetric>
            {
                new("学生总数", "486", "覆盖初二年级 10 个班"),
                new("平均分", "452.6", "较上次考试 +8.4"),
                new("优势学生", "128", "至少 2 门学科显著领先"),
                new("需关注", "37", "连续两次低于预警线")
            },
            SampleDataService.GetSubjectInsights());
    }
}

public sealed record DashboardMetric(string Label, string Value, string Hint);

public sealed record DashboardViewData(
    ObservableCollection<DashboardMetric> Metrics,
    ObservableCollection<SubjectInsight> Subjects);
