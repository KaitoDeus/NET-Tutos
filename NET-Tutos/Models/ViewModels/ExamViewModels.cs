using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class ExamTopicItem
{
    public int? CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "primary";
    public string IconClass { get; set; } = "bi-mortarboard";
    public int QuestionCount { get; set; } = 15;
    public int DurationMinutes { get; set; } = 15;
    public int PassingScorePercent { get; set; } = 80;
    public bool HasPassed { get; set; }
    public double? BestScore { get; set; }
    public Certificate? Certificate { get; set; }
}

public class ExamListViewModel
{
    public IEnumerable<ExamTopicItem> AvailableExams { get; set; } = new List<ExamTopicItem>();
    public IEnumerable<QuizAttempt> RecentAttempts { get; set; } = new List<QuizAttempt>();
    public IEnumerable<Certificate> Certificates { get; set; } = new List<Certificate>();
}

public class ExamQuestionItem
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string? CodeSnippet { get; set; }
}

public class ExamSessionViewModel
{
    public int? CategoryId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 15;
    public List<ExamQuestionItem> Questions { get; set; } = new();
}

public class ExamSubmissionViewModel
{
    public int? CategoryId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int ElapsedSeconds { get; set; }
    public Dictionary<int, string> Answers { get; set; } = new();
}

public class ExamQuestionResultItem
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string SelectedOption { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = string.Empty;
    public bool IsCorrect => SelectedOption.Equals(CorrectOption, StringComparison.OrdinalIgnoreCase);
    public string? Explanation { get; set; }
}

public class ExamResultViewModel
{
    public int AttemptId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public int DurationSeconds { get; set; }
    public Certificate? Certificate { get; set; }
    public List<ExamQuestionResultItem> QuestionResults { get; set; } = new();
}

public class CertificateVerifyViewModel
{
    public bool IsFound { get; set; }
    public Certificate? Certificate { get; set; }
}
