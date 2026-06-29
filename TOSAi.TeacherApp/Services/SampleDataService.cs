using System.Collections.ObjectModel;
using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public static class SampleDataService
{
    public static ObservableCollection<StudentSummary> GetStudents() => new()
    {
        new StudentSummary
        {
            StudentId = "20260101",
            Name = "李明",
            ClassName = "初二 1 班",
            TotalScore = 468,
            GradeRank = 12,
            StrongSubject = "数学",
            WeakSubject = "英语",
            Advice = "保持数学优势，英语阅读需要稳定训练。"
        },
        new StudentSummary
        {
            StudentId = "20260102",
            Name = "王雨",
            ClassName = "初二 1 班",
            TotalScore = 451,
            GradeRank = 25,
            StrongSubject = "英语",
            WeakSubject = "物理",
            Advice = "物理概念题失分较多，建议建立错题标签。"
        },
        new StudentSummary
        {
            StudentId = "20260103",
            Name = "陈然",
            ClassName = "初二 2 班",
            TotalScore = 432,
            GradeRank = 47,
            StrongSubject = "语文",
            WeakSubject = "化学",
            Advice = "化学基础公式和实验现象需要回补。"
        },
        new StudentSummary
        {
            StudentId = "20260104",
            Name = "赵晴",
            ClassName = "初二 2 班",
            TotalScore = 489,
            GradeRank = 4,
            StrongSubject = "英语",
            WeakSubject = "语文",
            Advice = "整体稳定，语文作文表达可继续提升。"
        }
    };

    public static ObservableCollection<ScoreRecord> GetScores() => new()
    {
        new ScoreRecord { StudentName = "李明", ClassName = "初二 1 班", Chinese = 91, Math = 98, English = 82, Physics = 96, Chemistry = 101 },
        new ScoreRecord { StudentName = "王雨", ClassName = "初二 1 班", Chinese = 88, Math = 92, English = 95, Physics = 80, Chemistry = 96 },
        new ScoreRecord { StudentName = "陈然", ClassName = "初二 2 班", Chinese = 96, Math = 86, English = 90, Physics = 85, Chemistry = 75 },
        new ScoreRecord { StudentName = "赵晴", ClassName = "初二 2 班", Chinese = 90, Math = 99, English = 104, Physics = 97, Chemistry = 99 }
    };

    public static ObservableCollection<SubjectInsight> GetSubjectInsights() => new()
    {
        new SubjectInsight { SubjectName = "语文", ClassAverage = 91.2, GradeAverage = 89.5, AdvantageCount = 18, RiskCount = 6, Comment = "阅读题表现较稳，作文区分度偏高。" },
        new SubjectInsight { SubjectName = "数学", ClassAverage = 93.8, GradeAverage = 90.1, AdvantageCount = 24, RiskCount = 5, Comment = "函数和几何综合题是主要拉分点。" },
        new SubjectInsight { SubjectName = "英语", ClassAverage = 92.7, GradeAverage = 91.9, AdvantageCount = 20, RiskCount = 8, Comment = "完形填空波动明显，需关注词汇基础。" },
        new SubjectInsight { SubjectName = "物理", ClassAverage = 89.5, GradeAverage = 88.4, AdvantageCount = 14, RiskCount = 10, Comment = "实验探究题需要加强审题和步骤表达。" },
        new SubjectInsight { SubjectName = "化学", ClassAverage = 91.6, GradeAverage = 90.7, AdvantageCount = 17, RiskCount = 7, Comment = "基础题稳定，推断题存在失分集中。" }
    };
}
