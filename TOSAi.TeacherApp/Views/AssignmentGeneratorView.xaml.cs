using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using TOSAi.TeacherApp.Models;
using TOSAi.TeacherApp.Services;

namespace TOSAi.TeacherApp.Views;

public partial class AssignmentGeneratorView : UserControl
{
    private static readonly string[] Topics = ["孟德尔遗传", "基因表达", "自然选择", "物种形成"];
    private static readonly string[] Directions = ["归纳和概括", "演绎和推理", "模型与建模", "批判性思维和创造性思维"];
    private static readonly string[] Scenarios = ["遗传病调查", "家系分析", "抗生素耐药性", "物种进化案例", "基因编辑伦理问题"];
    private static readonly string[] Difficulties = ["基础理解", "应用推理", "综合分析", "拓展创新"];
    private static readonly string[] QuestionTypes = ["选择题", "大题"];
    private static readonly string[] BankHeader = ["题型", "主题", "考察方向", "情景分类", "难度", "题干", "选项A", "选项B", "选项C", "选项D", "答案", "解析"];

    private readonly ObservableCollection<GeneratedQuestion> _questions = [];
    private readonly AiSettingsStore _settingsStore = new();
    private readonly IQuestionGenerationService _mockQuestionGenerationService = new MockQuestionGenerationService();
    private readonly IQuestionBankStore _questionBankStore = new HttpQuestionBankStore(ApiEndpointOptions.BaseUrl);
    private readonly IQuestionDraftStore _questionDraftStore = new HttpQuestionDraftStore(ApiEndpointOptions.BaseUrl);
    private readonly ObservableCollection<QuestionBankItem> _questionBank = [];
    private readonly ObservableCollection<QuestionBankItem> _filteredQuestionBank = [];
    private readonly ObservableCollection<QuestionDraft> _questionDrafts = [];
    private int _replaceIndex = -1;
    private string _assignmentTitle = "高中生物遗传与进化专题作业";
    private string _assignmentMeta = "尚未生成作业。";

    public AssignmentGeneratorView()
    {
        InitializeComponent();
        InitializeSelectors();
        QuestionsItemsControl.ItemsSource = _questions;
        BankDataGrid.ItemsSource = _filteredQuestionBank;
        DraftsDataGrid.ItemsSource = _questionDrafts;
        RefreshBankFilters();
        RefreshBankView();
        Loaded += AssignmentGeneratorView_Loaded;
    }


    private async void AssignmentGeneratorView_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= AssignmentGeneratorView_Loaded;
        await LoadCloudBankAsync(showEmptyMessage: false);
        await LoadQuestionDraftsAsync(showError: false);
    }

    private void InitializeSelectors()
    {
        TopicListBox.ItemsSource = Topics;
        DirectionListBox.ItemsSource = Directions;
        ScenarioListBox.ItemsSource = Scenarios;
        DifficultyComboBox.ItemsSource = Difficulties;
        DifficultyComboBox.SelectedIndex = 1;

        ReplaceTypeComboBox.ItemsSource = QuestionTypes;
        ReplaceDifficultyComboBox.ItemsSource = Difficulties;
        ReplaceTopicComboBox.ItemsSource = Topics;
        ReplaceScenarioComboBox.ItemsSource = Scenarios;

        SelectDefaults(TopicListBox, 0, 1);
        SelectDefaults(DirectionListBox, 1, 2);
        SelectDefaults(ScenarioListBox, 0, 1);
    }

    private static void SelectDefaults(ListBox listBox, params int[] indexes)
    {
        foreach (int index in indexes)
        {
            if (index >= 0 && index < listBox.Items.Count)
            {
                listBox.SelectedItems.Add(listBox.Items[index]);
            }
        }
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadPositiveNumber(ChoiceCountTextBox.Text, "选择题数量", out int choiceCount) ||
            !TryReadPositiveNumber(EssayCountTextBox.Text, "大题数量", out int essayCount))
        {
            return;
        }

        string[] selectedTopics = GetSelectedItems(TopicListBox, Topics);
        string[] selectedDirections = GetSelectedItems(DirectionListBox, Directions);
        string[] selectedScenarios = GetSelectedItems(ScenarioListBox, Scenarios);
        string difficulty = DifficultyComboBox.SelectedItem?.ToString() ?? Difficulties[1];

        _questions.Clear();
        int questionNumber = 1;
        for (int i = 0; i < choiceCount; i++)
        {
            _questions.Add(CreateQuestion(questionNumber++, "选择题", difficulty, Pick(selectedTopics, i), Pick(selectedDirections, i), Pick(selectedScenarios, i), i));
        }

        for (int i = 0; i < essayCount; i++)
        {
            _questions.Add(CreateQuestion(questionNumber++, "大题", difficulty, Pick(selectedTopics, i + choiceCount), Pick(selectedDirections, i), Pick(selectedScenarios, i + choiceCount), i));
        }

        _assignmentTitle = "高中生物遗传与进化专题作业";
        _assignmentMeta = $"主题：{string.Join("、", selectedTopics)}；方向：{string.Join("、", selectedDirections)}；情景：{string.Join("、", selectedScenarios)}；难度：{difficulty}；共 {_questions.Count} 题。";
        PreviewTitleText.Text = _assignmentTitle;
        PreviewMetaText.Text = _assignmentMeta;
        StatusText.Text = _questionBank.Count > 0
            ? $"已生成 {_questions.Count} 道题，优先使用已导入题库。"
            : $"已生成 {_questions.Count} 道题，当前使用内置模板。";
        ReplacePanel.Visibility = Visibility.Collapsed;
        _replaceIndex = -1;
    }

    private async void AiGenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadPositiveNumber(ChoiceCountTextBox.Text, "\u9009\u62e9\u9898\u6570\u91cf", out int choiceCount) ||
            !TryReadPositiveNumber(EssayCountTextBox.Text, "\u5927\u9898\u6570\u91cf", out int essayCount))
        {
            return;
        }

        int totalCount = choiceCount + essayCount;
        if (totalCount == 0)
        {
            MessageBox.Show("\u8bf7\u81f3\u5c11\u8bbe\u7f6e\u4e00\u9053\u8981\u751f\u6210\u7684\u9898\u76ee\u3002", "AI \u751f\u6210\u65b0\u9898", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (totalCount > 10)
        {
            MessageBox.Show("AI \u751f\u6210\u65b0\u9898\u4e00\u6b21\u6700\u591a\u751f\u6210 10 \u9053\uff0c\u8bf7\u51cf\u5c11\u9009\u62e9\u9898\u6216\u5927\u9898\u6570\u91cf\u3002", "AI \u751f\u6210\u65b0\u9898", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_questionBank.Count == 0)
        {
            await LoadCloudBankAsync(showEmptyMessage: true);
        }

        if (_questionBank.Count == 0)
        {
            MessageBox.Show("\u4e91\u7aef\u9898\u5e93\u8fd8\u6ca1\u6709\u53ef\u53c2\u8003\u7684\u9898\u76ee\u3002\u8bf7\u5148\u5728\u201c\u9898\u5e93\u7ba1\u7406\u201d\u4e2d\u5bfc\u5165\u5e76\u4fdd\u5b58\u9898\u5e93\u3002", "AI \u751f\u6210\u65b0\u9898", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string[] selectedTopics = GetSelectedItems(TopicListBox, Topics);
        string[] selectedDirections = GetSelectedItems(DirectionListBox, Directions);
        string[] selectedScenarios = GetSelectedItems(ScenarioListBox, Scenarios);
        string difficulty = DifficultyComboBox.SelectedItem?.ToString() ?? Difficulties[1];
        IReadOnlyList<QuestionGenerationReference> references = BuildGenerationReferences(selectedTopics, selectedDirections, selectedScenarios, difficulty);

        AiGenerateButton.IsEnabled = false;
        StatusText.Text = "\u6b63\u5728\u57fa\u4e8e\u4e91\u7aef\u9898\u5e93\u751f\u6210 AI \u65b0\u9898\u8349\u7a3f...";

        try
        {
            AiSettings settings = await _settingsStore.LoadAsync();
            IQuestionGenerationService generationService = settings.UseMockAnalysis
                ? _mockQuestionGenerationService
                : new OpenAiCompatibleQuestionGenerationService(settings);

            QuestionGenerationRequest request = new(difficulty, selectedTopics, selectedDirections, selectedScenarios, choiceCount, essayCount, references);
            IReadOnlyList<GeneratedQuestionDraft> drafts = await generationService.GenerateAsync(request);
            string sourcePrompt = BuildDraftSourcePrompt(selectedTopics, selectedDirections, selectedScenarios, difficulty, choiceCount, essayCount, references.Count, settings.UseMockAnalysis);

            foreach (GeneratedQuestionDraft draft in drafts)
            {
                await _questionDraftStore.SaveAsync(ToQuestionDraft(draft, sourcePrompt, references.Count));
            }

            await LoadQuestionDraftsAsync(showError: true);
            StatusText.Text = settings.UseMockAnalysis
                ? $"\u5df2\u7528\u6a21\u62df AI \u751f\u6210 {drafts.Count} \u9053\u65b0\u9898\uff0c\u5df2\u8fdb\u5165\u201cAI \u8349\u7a3f\u6c60\u201d\u5f85\u5ba1\u6838\u3002"
                : $"\u5df2\u57fa\u4e8e {references.Count} \u9053\u4e91\u7aef\u53c2\u8003\u9898\u751f\u6210 {drafts.Count} \u9053\u65b0\u9898\uff0c\u5df2\u8fdb\u5165\u201cAI \u8349\u7a3f\u6c60\u201d\u5f85\u5ba1\u6838\u3002";
            ReplacePanel.Visibility = Visibility.Collapsed;
            _replaceIndex = -1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            MessageBox.Show(ex.Message, "AI \u751f\u6210\u65b0\u9898\u5931\u8d25", MessageBoxButton.OK, MessageBoxImage.Warning);
            StatusText.Text = "AI \u751f\u6210\u65b0\u9898\u5931\u8d25\u3002";
        }
        finally
        {
            AiGenerateButton.IsEnabled = true;
        }
    }
    private IReadOnlyList<QuestionGenerationReference> BuildGenerationReferences(IReadOnlyList<string> topics, IReadOnlyList<string> directions, IReadOnlyList<string> scenarios, string difficulty)
    {
        List<QuestionBankItem> candidates = _questionBank
            .Where(item => topics.Contains(item.Topic) || directions.Contains(item.Direction) || scenarios.Contains(item.Scenario) || item.Difficulty == difficulty)
            .OrderByDescending(item => topics.Contains(item.Topic))
            .ThenByDescending(item => item.Difficulty == difficulty)
            .ThenByDescending(item => directions.Contains(item.Direction))
            .ThenByDescending(item => scenarios.Contains(item.Scenario))
            .Take(8)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = _questionBank.Take(8).ToList();
        }

        return candidates.Select(ToGenerationReference).ToList();
    }

    private static QuestionGenerationReference ToGenerationReference(QuestionBankItem item)
    {
        return new QuestionGenerationReference(
            item.Type,
            item.Topic,
            item.Direction,
            item.Scenario,
            item.Difficulty,
            item.Stem,
            item.Options.Select(option => option.Text).ToList(),
            item.Answer,
            item.Explanation);
    }

    private static GeneratedQuestion ToGeneratedQuestion(GeneratedQuestionDraft draft, int number)
    {
        ObservableCollection<QuestionOption> options = new(draft.Options.Select(option => new QuestionOption(option)));
        return new GeneratedQuestion(
            number,
            draft.Type,
            draft.Difficulty,
            draft.Topic,
            draft.Direction,
            draft.Scenario,
            draft.Stem,
            options,
            draft.Answer);
    }

    private static QuestionDraft ToQuestionDraft(GeneratedQuestionDraft draft, string sourcePrompt, int referenceCount)
    {
        List<string> options = draft.Options.Take(4).ToList();
        while (options.Count < 4)
        {
            options.Add(string.Empty);
        }

        return new QuestionDraft
        {
            Status = "\u5f85\u5ba1\u6838",
            Type = draft.Type,
            Topic = draft.Topic,
            Direction = draft.Direction,
            Scenario = draft.Scenario,
            Difficulty = draft.Difficulty,
            Stem = draft.Stem,
            OptionA = options[0],
            OptionB = options[1],
            OptionC = options[2],
            OptionD = options[3],
            Answer = draft.Answer,
            SourcePrompt = sourcePrompt,
            ReferenceCount = referenceCount
        };
    }

    private static GeneratedQuestion ToGeneratedQuestion(QuestionDraft draft, int number)
    {
        return new GeneratedQuestion(
            number,
            draft.Type,
            draft.Difficulty,
            draft.Topic,
            draft.Direction,
            draft.Scenario,
            draft.Stem,
            draft.ToOptions(),
            draft.Answer);
    }

    private static string BuildDraftSourcePrompt(IReadOnlyList<string> topics, IReadOnlyList<string> directions, IReadOnlyList<string> scenarios, string difficulty, int choiceCount, int essayCount, int referenceCount, bool usedMock)
    {
        StringBuilder builder = new();
        builder.AppendLine($"Difficulty: {difficulty}");
        builder.AppendLine($"Topics: {string.Join(", ", topics)}");
        builder.AppendLine($"Directions: {string.Join(", ", directions)}");
        builder.AppendLine($"Scenarios: {string.Join(", ", scenarios)}");
        builder.AppendLine($"Choice count: {choiceCount}");
        builder.AppendLine($"Essay count: {essayCount}");
        builder.AppendLine($"Reference count: {referenceCount}");
        builder.AppendLine($"Generator: {(usedMock ? "mock" : "ai")}");
        return builder.ToString().Trim();
    }

    private async Task LoadQuestionDraftsAsync(bool showError)
    {
        try
        {
            ObservableCollection<QuestionDraft> rows = await _questionDraftStore.LoadAsync();
            _questionDrafts.Clear();
            foreach (QuestionDraft row in rows)
            {
                _questionDrafts.Add(row);
            }

            DraftStatusText.Text = _questionDrafts.Count == 0
                ? "\u6682\u65e0 AI \u9898\u76ee\u8349\u7a3f\u3002"
                : $"\u5df2\u8bfb\u53d6 {_questionDrafts.Count} \u9053 AI \u9898\u76ee\u8349\u7a3f\u3002";
            DraftsDataGrid.SelectedIndex = _questionDrafts.Count > 0 ? 0 : -1;
            if (_questionDrafts.Count == 0)
            {
                ShowQuestionDraft(null);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException or JsonException)
        {
            DraftStatusText.Text = "AI \u8349\u7a3f\u6c60\u8bfb\u53d6\u5931\u8d25\u3002";
            if (showError)
            {
                MessageBox.Show(ex.Message, "\u8bfb\u53d6 AI \u8349\u7a3f\u6c60\u5931\u8d25", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void RefreshDraftsButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadQuestionDraftsAsync(showError: true);
    }

    private void DraftsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ShowQuestionDraft(DraftsDataGrid.SelectedItem as QuestionDraft);
    }

    private async void ConfirmDraftButton_Click(object sender, RoutedEventArgs e)
    {
        if (DraftsDataGrid.SelectedItem is not QuestionDraft draft)
        {
            MessageBox.Show("\u8bf7\u5148\u9009\u62e9\u4e00\u9053\u9898\u76ee\u8349\u7a3f\u3002", "AI \u8349\u7a3f\u6c60", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            int nextNumber = _questions.Count + 1;
            _questions.Add(ToGeneratedQuestion(draft, nextNumber));
            await _questionDraftStore.UpdateStatusAsync(draft.Id, "\u5df2\u786e\u8ba4");
            await LoadQuestionDraftsAsync(showError: false);
            PreviewTitleText.Text = _assignmentTitle;
            _assignmentMeta = $"\u5171 {_questions.Count} \u9898\uff0c\u5305\u542b\u5df2\u5ba1\u6838\u7684 AI \u9898\u76ee\u8349\u7a3f\u3002";
            PreviewMetaText.Text = _assignmentMeta;
            StatusText.Text = $"\u5df2\u786e\u8ba4\u8349\u7a3f\u5e76\u52a0\u5165\u4f5c\u4e1a\u9884\u89c8\uff1a\u7b2c {nextNumber} \u9898\u3002";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException or JsonException)
        {
            MessageBox.Show(ex.Message, "\u786e\u8ba4\u9898\u76ee\u8349\u7a3f\u5931\u8d25", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DiscardDraftButton_Click(object sender, RoutedEventArgs e)
    {
        if (DraftsDataGrid.SelectedItem is not QuestionDraft draft)
        {
            MessageBox.Show("\u8bf7\u5148\u9009\u62e9\u4e00\u9053\u9898\u76ee\u8349\u7a3f\u3002", "AI \u8349\u7a3f\u6c60", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await _questionDraftStore.UpdateStatusAsync(draft.Id, "\u5df2\u5e9f\u5f03");
            await LoadQuestionDraftsAsync(showError: false);
            StatusText.Text = "\u5df2\u5c06\u9898\u76ee\u8349\u7a3f\u6807\u8bb0\u4e3a\u5df2\u5e9f\u5f03\u3002";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException or JsonException)
        {
            MessageBox.Show(ex.Message, "\u5e9f\u5f03\u9898\u76ee\u8349\u7a3f\u5931\u8d25", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteDraftButton_Click(object sender, RoutedEventArgs e)
    {
        if (DraftsDataGrid.SelectedItem is not QuestionDraft draft)
        {
            MessageBox.Show("\u8bf7\u5148\u9009\u62e9\u4e00\u9053\u9898\u76ee\u8349\u7a3f\u3002", "AI \u8349\u7a3f\u6c60", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBoxResult result = MessageBox.Show("\u786e\u5b9a\u8981\u5220\u9664\u8fd9\u9053\u9898\u76ee\u8349\u7a3f\u5417\uff1f", "\u5220\u9664\u9898\u76ee\u8349\u7a3f", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _questionDraftStore.DeleteAsync(draft.Id);
            await LoadQuestionDraftsAsync(showError: false);
            StatusText.Text = "\u9898\u76ee\u8349\u7a3f\u5df2\u5220\u9664\u3002";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        {
            MessageBox.Show(ex.Message, "\u5220\u9664\u9898\u76ee\u8349\u7a3f\u5931\u8d25", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ShowQuestionDraft(QuestionDraft? draft)
    {
        DraftStemTextBox.Text = draft?.Stem ?? string.Empty;
        DraftOptionsTextBox.Text = draft?.OptionsText ?? string.Empty;
        DraftAnswerTextBox.Text = draft?.Answer ?? string.Empty;
        DraftSourceTextBox.Text = draft?.SourcePrompt ?? string.Empty;
    }

    private static bool TryReadPositiveNumber(string text, string label, out int value)
    {
        if (int.TryParse(text.Trim(), out value) && value >= 0 && value <= 50)
        {
            return true;
        }

        MessageBox.Show($"{label}请输入 0 到 50 之间的整数。", "生成作业", MessageBoxButton.OK, MessageBoxImage.Warning);
        value = 0;
        return false;
    }

    private static string[] GetSelectedItems(ListBox listBox, string[] fallback)
    {
        string[] selected = listBox.SelectedItems.Cast<object>().Select(item => item.ToString() ?? string.Empty).Where(item => item.Length > 0).ToArray();
        return selected.Length > 0 ? selected : fallback;
    }

    private static string Pick(IReadOnlyList<string> values, int index) => values[index % values.Count];

    private GeneratedQuestion CreateQuestion(int number, string type, string difficulty, string topic, string direction, string scenario, int variant)
    {
        QuestionBankItem? bankItem = FindBankItem(type, difficulty, topic, direction, scenario, variant);
        if (bankItem is not null)
        {
            return bankItem.ToGeneratedQuestion(number);
        }

        return type == "选择题"
            ? CreateChoiceQuestion(number, difficulty, topic, direction, scenario)
            : CreateEssayQuestion(number, difficulty, topic, direction, scenario);
    }

    private QuestionBankItem? FindBankItem(string type, string difficulty, string topic, string direction, string scenario, int variant)
    {
        List<QuestionBankItem> candidates = _questionBank
            .Where(item => item.Type == type)
            .Where(item => item.Difficulty == difficulty)
            .Where(item => item.Topic == topic)
            .Where(item => item.Scenario == scenario)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = _questionBank
                .Where(item => item.Type == type)
                .Where(item => item.Topic == topic)
                .Where(item => item.Difficulty == difficulty || item.Direction == direction || item.Scenario == scenario)
                .ToList();
        }

        if (candidates.Count == 0)
        {
            candidates = _questionBank
                .Where(item => item.Type == type)
                .Where(item => item.Topic == topic || item.Difficulty == difficulty)
                .ToList();
        }

        return candidates.Count == 0 ? null : candidates[variant % candidates.Count];
    }

    private static GeneratedQuestion CreateChoiceQuestion(int number, string difficulty, string topic, string direction, string scenario)
    {
        string stem = $"在“{scenario}”情境中，围绕“{topic}”设计一次课堂讨论。下列哪一项最符合“{direction}”的考察要求？";
        ObservableCollection<QuestionOption> options =
        [
            new("A. 只复述教材中的定义，不结合题干证据。"),
            new($"B. 根据情境信息提取关键变量，并用“{topic}”的核心概念解释现象。"),
            new("C. 忽略样本数量和变量控制，直接给出唯一结论。"),
            new("D. 只讨论社会影响，不分析相关生物学机制。")
        ];
        string answer = $"答案：B。解析：本题难度为“{difficulty}”，关键是把情境证据、核心概念和思维方向对应起来，避免脱离材料作答。";

        return new GeneratedQuestion(number, "选择题", difficulty, topic, direction, scenario, stem, options, answer);
    }

    private static GeneratedQuestion CreateEssayQuestion(int number, string difficulty, string topic, string direction, string scenario)
    {
        string stem = $"请基于“{scenario}”情境，围绕“{topic}”完成分析任务：1. 提取材料中的关键生物学信息；2. 说明相关机制或模型；3. 按“{direction}”的要求提出判断或解释；4. 写出一个可能的限制条件。";
        string answer = $"参考答案：应包含情境证据、{topic} 的核心机制、清晰的推理链和限制条件。难度为“{difficulty}”时，评分重点放在证据选择、概念准确性、推理完整性和表达规范。";

        return new GeneratedQuestion(number, "大题", difficulty, topic, direction, scenario, stem, [], answer);
    }

    private void ReplaceQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: GeneratedQuestion question })
        {
            return;
        }

        _replaceIndex = _questions.IndexOf(question);
        if (_replaceIndex < 0)
        {
            return;
        }

        ReplaceTitleText.Text = $"更换第 {question.Number} 题";
        ReplaceTypeComboBox.SelectedItem = question.Type;
        ReplaceDifficultyComboBox.SelectedItem = question.Difficulty;
        ReplaceTopicComboBox.SelectedItem = question.Topic;
        ReplaceScenarioComboBox.SelectedItem = question.Scenario;
        ReplacePanel.Visibility = Visibility.Visible;
    }

    private void ApplyReplaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_replaceIndex < 0 || _replaceIndex >= _questions.Count)
        {
            return;
        }

        GeneratedQuestion oldQuestion = _questions[_replaceIndex];
        string type = ReplaceTypeComboBox.SelectedItem?.ToString() ?? oldQuestion.Type;
        string difficulty = ReplaceDifficultyComboBox.SelectedItem?.ToString() ?? oldQuestion.Difficulty;
        string topic = ReplaceTopicComboBox.SelectedItem?.ToString() ?? oldQuestion.Topic;
        string scenario = ReplaceScenarioComboBox.SelectedItem?.ToString() ?? oldQuestion.Scenario;
        string direction = oldQuestion.Direction;

        _questions[_replaceIndex] = CreateQuestion(oldQuestion.Number, type, difficulty, topic, direction, scenario, _replaceIndex + 1);
        StatusText.Text = $"已按条件更换第 {oldQuestion.Number} 题。";
        ReplacePanel.Visibility = Visibility.Collapsed;
        _replaceIndex = -1;
    }

    private void CancelReplaceButton_Click(object sender, RoutedEventArgs e)
    {
        ReplacePanel.Visibility = Visibility.Collapsed;
        _replaceIndex = -1;
    }

    private void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_questions.Count == 0)
        {
            MessageBox.Show("请先生成作业。", "下载 HTML", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = "下载作业 HTML",
            Filter = "HTML 文件 (*.html)|*.html",
            FileName = "高中生物遗传与进化专题作业.html",
            AddExtension = true,
            DefaultExt = ".html"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, BuildHtml(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            StatusText.Text = $"HTML 已下载：{dialog.FileName}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "下载失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportBankTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new()
        {
            Title = "下载题库模板",
            Filter = "CSV 文件 (*.csv)|*.csv",
            FileName = "题库导入模板.csv",
            AddExtension = true,
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            string[] lines =
            [
                ToCsvLine(BankHeader),
                ToCsvLine(["选择题", "孟德尔遗传", "演绎和推理", "家系分析", "应用推理", "某遗传病家系中，亲代表现正常但子代出现患病个体。下列判断最合理的是？", "A. 该病一定为伴性遗传", "B. 该病可能为隐性遗传", "C. 该病一定由环境引起", "D. 无法根据家系继续推理", "B", "亲代表现正常而子代患病，符合隐性遗传的典型推理起点。"]),
                ToCsvLine(["大题", "自然选择", "模型与建模", "抗生素耐药性", "综合分析", "请用自然选择模型解释某细菌种群在抗生素使用后耐药性比例升高的原因。", "", "", "", "", "参考答案", "应说明变异先存在、抗生素作为选择压力、耐药个体留下更多后代、种群基因频率改变。"])
            ];
            File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            BankStatusText.Text = $"题库模板已下载：{dialog.FileName}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "下载模板失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ImportBankButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "导入题库 CSV",
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            List<QuestionBankItem> rows = ImportQuestionBank(dialog.FileName);
            _questionBank.Clear();
            foreach (QuestionBankItem row in rows)
            {
                _questionBank.Add(row);
            }

            RefreshBankFilters();
            RefreshBankView();
            BankStatusText.Text = $"已导入 {_questionBank.Count} 道题：{Path.GetFileName(dialog.FileName)}。请点击“保存云端”同步到服务器。";
            StatusText.Text = "已导入题库，保存云端后生成作业和更换题目会优先使用云端题库。";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "导入题库失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SaveBankButton_Click(object sender, RoutedEventArgs e)
    {
        if (_questionBank.Count == 0)
        {
            MessageBox.Show("还没有导入题库。", "保存题库", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            int importedCount = _questionBank.Count;
            await _questionBankStore.SaveAsync(_questionBank);
            await LoadCloudBankAsync(showEmptyMessage: false);
            BankStatusText.Text = $"已合并保存 {importedCount} 道题到云端；当前云端题库共 {_questionBank.Count} 道。";
            StatusText.Text = "云端题库已更新，生成作业会优先使用这些题目。";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        {
            MessageBox.Show(ex.Message, "保存云端题库失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void LoadBankButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadCloudBankAsync(showEmptyMessage: true);
    }

    private async void ClearBankButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show("确定要清空云端保存的题库吗？", "清空云端题库", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _questionBankStore.ClearAsync();
            _questionBank.Clear();
            RefreshBankFilters();
            RefreshBankView();
            BankStatusText.Text = "云端题库已清空。";
            StatusText.Text = "云端题库已清空，生成作业会回退到内置模板。";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        {
            MessageBox.Show(ex.Message, "清空云端题库失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadCloudBankAsync(bool showEmptyMessage)
    {
        try
        {
            ObservableCollection<QuestionBankItem> rows = await _questionBankStore.LoadAsync();
            _questionBank.Clear();
            foreach (QuestionBankItem row in rows)
            {
                _questionBank.Add(row);
            }

            RefreshBankFilters();
            RefreshBankView();
            if (_questionBank.Count > 0)
            {
                BankStatusText.Text = $"已读取云端题库 {_questionBank.Count} 道题。";
                StatusText.Text = "已加载云端题库，生成作业会优先使用这些题目。";
            }
            else if (showEmptyMessage)
            {
                BankStatusText.Text = "云端还没有保存题库。";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or HttpRequestException or TaskCanceledException)
        {
            MessageBox.Show(ex.Message, "读取云端题库失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BankFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshBankView();
    }

    private void RefreshBankFilters()
    {
        SetFilterItems(BankTypeFilterComboBox, _questionBank.Select(item => item.Type).Concat(QuestionTypes));
        SetFilterItems(BankTopicFilterComboBox, _questionBank.Select(item => item.Topic).Concat(Topics));
        SetFilterItems(BankDifficultyFilterComboBox, _questionBank.Select(item => item.Difficulty).Concat(Difficulties));
    }

    private static void SetFilterItems(ComboBox comboBox, IEnumerable<string> values)
    {
        string current = comboBox.SelectedItem?.ToString() ?? "全部";
        List<string> items = ["全部", .. values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().OrderBy(value => value)];
        comboBox.ItemsSource = items;
        comboBox.SelectedItem = items.Contains(current) ? current : "全部";
    }

    private void RefreshBankView()
    {
        string type = BankTypeFilterComboBox.SelectedItem?.ToString() ?? "全部";
        string topic = BankTopicFilterComboBox.SelectedItem?.ToString() ?? "全部";
        string difficulty = BankDifficultyFilterComboBox.SelectedItem?.ToString() ?? "全部";

        List<QuestionBankItem> rows = _questionBank
            .Where(item => type == "全部" || item.Type == type)
            .Where(item => topic == "全部" || item.Topic == topic)
            .Where(item => difficulty == "全部" || item.Difficulty == difficulty)
            .ToList();

        _filteredQuestionBank.Clear();
        foreach (QuestionBankItem row in rows)
        {
            _filteredQuestionBank.Add(row);
        }

        BankStatusText.Text = _questionBank.Count == 0
            ? "当前未导入题库。"
            : $"题库共 {_questionBank.Count} 道题，当前筛选显示 {_filteredQuestionBank.Count} 道。";
    }

    private static List<QuestionBankItem> ImportQuestionBank(string filePath)
    {
        using TextFieldParser parser = new(filePath, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true
        };
        parser.SetDelimiters(",");

        string[]? header = parser.ReadFields();
        ValidateBankHeader(header);

        List<QuestionBankItem> rows = [];
        int rowNumber = 1;
        while (!parser.EndOfData)
        {
            rowNumber++;
            string[]? fields = parser.ReadFields();
            if (fields is null || fields.Length == 0 || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (fields.Length != BankHeader.Length)
            {
                throw new InvalidDataException($"第 {rowNumber} 行字段数量不正确，应为 {BankHeader.Length} 列。");
            }

            rows.Add(ParseBankRow(fields, rowNumber));
        }

        if (rows.Count == 0)
        {
            throw new InvalidDataException("题库文件没有可导入的题目。");
        }

        return rows;
    }

    private static void ValidateBankHeader(string[]? header)
    {
        if (header is null || header.Length != BankHeader.Length)
        {
            throw new InvalidDataException("题库表头不正确，请先下载题库模板。");
        }

        for (int i = 0; i < BankHeader.Length; i++)
        {
            if (!string.Equals(header[i].Trim(), BankHeader[i], StringComparison.Ordinal))
            {
                throw new InvalidDataException($"第 {i + 1} 列应为“{BankHeader[i]}”，当前为“{header[i]}”。");
            }
        }
    }

    private static QuestionBankItem ParseBankRow(string[] fields, int rowNumber)
    {
        string type = Required(fields[0], rowNumber, "题型");
        string topic = Required(fields[1], rowNumber, "主题");
        string direction = Required(fields[2], rowNumber, "考察方向");
        string scenario = Required(fields[3], rowNumber, "情景分类");
        string difficulty = Required(fields[4], rowNumber, "难度");
        string stem = Required(fields[5], rowNumber, "题干");
        string answer = Required(fields[10], rowNumber, "答案");
        string explanation = fields[11].Trim();

        if (!QuestionTypes.Contains(type))
        {
            throw new InvalidDataException($"第 {rowNumber} 行题型必须是“选择题”或“大题”。");
        }

        ObservableCollection<QuestionOption> options = [];
        for (int i = 6; i <= 9; i++)
        {
            string option = fields[i].Trim();
            if (!string.IsNullOrWhiteSpace(option))
            {
                options.Add(new QuestionOption(option));
            }
        }

        if (type == "选择题" && options.Count == 0)
        {
            throw new InvalidDataException($"第 {rowNumber} 行选择题至少需要填写一个选项。");
        }

        return new QuestionBankItem(type, topic, direction, scenario, difficulty, stem, options, answer, explanation);
    }

    private static string Required(string value, int rowNumber, string columnName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"第 {rowNumber} 行“{columnName}”不能为空。");
        }

        return value.Trim();
    }

    private string BuildHtml()
    {
        StringBuilder builder = new();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"zh-CN\">");
        builder.AppendLine("<head><meta charset=\"utf-8\">");
        builder.AppendLine($"<title>{EscapeHtml(_assignmentTitle)}</title>");
        builder.AppendLine("<style>body{font-family:'Microsoft YaHei',Arial,sans-serif;margin:40px;color:#111827;line-height:1.7;}h1{text-align:center;margin-bottom:8px}.meta{color:#4b5563;text-align:center;margin-bottom:30px}.question{break-inside:avoid;border-bottom:1px solid #e5e7eb;padding:0 0 18px;margin:0 0 20px}.tag{color:#2563eb;font-size:13px;margin-bottom:8px}.stem{font-weight:600}.options{margin-top:8px}.answer{margin:10px 0 0;color:#374151}.answers{page-break-before:always;margin-top:48px}</style>");
        builder.AppendLine("</head><body>");
        builder.AppendLine($"<h1>{EscapeHtml(_assignmentTitle)}</h1>");
        builder.AppendLine($"<div class=\"meta\">{EscapeHtml(_assignmentMeta)}</div>");

        foreach (GeneratedQuestion question in _questions)
        {
            builder.AppendLine("<section class=\"question\">");
            builder.AppendLine($"<div class=\"tag\">{EscapeHtml(question.Type)} | {EscapeHtml(question.Difficulty)} | {EscapeHtml(question.Topic)} | {EscapeHtml(question.Scenario)}</div>");
            builder.AppendLine($"<div class=\"stem\">{question.Number}. {EscapeHtml(question.Stem)}</div>");
            if (question.Options.Count > 0)
            {
                builder.AppendLine("<div class=\"options\">");
                foreach (QuestionOption option in question.Options)
                {
                    builder.AppendLine($"<div>{EscapeHtml(option.Text)}</div>");
                }
                builder.AppendLine("</div>");
            }
            builder.AppendLine("</section>");
        }

        builder.AppendLine("<section class=\"answers\">");
        builder.AppendLine("<h2>答案与解析</h2>");
        foreach (GeneratedQuestion question in _questions)
        {
            builder.AppendLine($"<div class=\"answer\"><strong>{question.Number}.</strong> {EscapeHtml(question.Answer)}</div>");
        }
        builder.AppendLine("</section></body></html>");
        return builder.ToString();
    }

    private static string ToCsvLine(IEnumerable<string> fields) => string.Join(",", fields.Select(EscapeCsv));

    private static string EscapeCsv(string field)
    {
        return field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r')
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}

public sealed record QuestionBankItem(
    string Type,
    string Topic,
    string Direction,
    string Scenario,
    string Difficulty,
    string Stem,
    ObservableCollection<QuestionOption> Options,
    string Answer,
    string Explanation)
{
    public GeneratedQuestion ToGeneratedQuestion(int number)
    {
        ObservableCollection<QuestionOption> copiedOptions = new(Options.Select(option => new QuestionOption(option.Text)));
        string fullAnswer = string.IsNullOrWhiteSpace(Explanation) ? $"答案：{Answer}" : $"答案：{Answer}。解析：{Explanation}";
        return new GeneratedQuestion(number, Type, Difficulty, Topic, Direction, Scenario, Stem, copiedOptions, fullAnswer);
    }
}

public sealed class GeneratedQuestion : INotifyPropertyChanged
{
    private string _stem;
    private string _answer;

    public GeneratedQuestion(int number, string type, string difficulty, string topic, string direction, string scenario, string stem, ObservableCollection<QuestionOption> options, string answer)
    {
        Number = number;
        Type = type;
        Difficulty = difficulty;
        Topic = topic;
        Direction = direction;
        Scenario = scenario;
        _stem = stem;
        Options = options;
        _answer = answer;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Number { get; }
    public string Type { get; }
    public string Difficulty { get; }
    public string Topic { get; }
    public string Direction { get; }
    public string Scenario { get; }
    public string Header => $"{Number}. {Type} | {Difficulty} | {Topic} | {Scenario}";

    public string Stem
    {
        get => _stem;
        set
        {
            if (_stem == value)
            {
                return;
            }

            _stem = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Stem)));
        }
    }

    public ObservableCollection<QuestionOption> Options { get; }

    public string Answer
    {
        get => _answer;
        set
        {
            if (_answer == value)
            {
                return;
            }

            _answer = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Answer)));
        }
    }
}

public sealed class QuestionOption : INotifyPropertyChanged
{
    private string _text;

    public QuestionOption(string text)
    {
        _text = text;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }
}
