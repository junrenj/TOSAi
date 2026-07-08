using System.Collections.ObjectModel;
using TOSAi.TeacherApp.Views;

namespace TOSAi.TeacherApp.Models;

public sealed class QuestionDraft
{
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string Status { get; set; } = "\u5f85\u5ba1\u6838";

    public string Type { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public string Direction { get; set; } = string.Empty;

    public string Scenario { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public string Stem { get; set; } = string.Empty;

    public string OptionA { get; set; } = string.Empty;

    public string OptionB { get; set; } = string.Empty;

    public string OptionC { get; set; } = string.Empty;

    public string OptionD { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string SourcePrompt { get; set; } = string.Empty;

    public int ReferenceCount { get; set; }

    public string CreatedAtText => CreatedAt == default ? string.Empty : CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string ShortStem => string.IsNullOrWhiteSpace(Stem) ? "No stem" : Stem;

    public string OptionsText => string.Join(Environment.NewLine, Options());

    public ObservableCollection<QuestionOption> ToOptions()
    {
        ObservableCollection<QuestionOption> options = [];
        foreach (string option in Options())
        {
            options.Add(new QuestionOption(option));
        }

        return options;
    }

    private IEnumerable<string> Options()
    {
        foreach (string option in new[] { OptionA, OptionB, OptionC, OptionD })
        {
            if (!string.IsNullOrWhiteSpace(option))
            {
                yield return option.Trim();
            }
        }
    }
}
