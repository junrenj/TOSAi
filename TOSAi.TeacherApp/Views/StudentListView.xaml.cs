using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class StudentListView : UserControl
{
    public StudentListView()
    {
        InitializeComponent();
        DataContext = new StudentListViewData(SampleDataService.GetStudents());
    }

    private void PlaceholderButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("该按钮已预留，可在 StudentListView.xaml.cs 中接入新增或导入逻辑。", "预留功能");
    }
}

public sealed record StudentListViewData(ObservableCollection<StudentSummary> Students);
