using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class QuizItemViewModel
{
    public int QuestionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string? SelectedOption { get; set; }
    public string CorrectOption { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public bool? IsCorrect => string.IsNullOrEmpty(SelectedOption) ? null : SelectedOption.Equals(CorrectOption, StringComparison.OrdinalIgnoreCase);
}

public class QuizSubmissionViewModel
{
    public int? TutorialId { get; set; }
    public string? TopicTitle { get; set; }
    public List<QuizItemViewModel> Questions { get; set; } = new();
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsSubmitted { get; set; }
}

