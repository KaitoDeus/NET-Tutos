using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class QuizController : Controller
{
    private readonly ITutorialService _tutorialService;

    public QuizController(ITutorialService tutorialService)
    {
        _tutorialService = tutorialService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _tutorialService.GetCategoriesWithTutorialsAsync();
        return View(categories);
    }

    public async Task<IActionResult> Take(int? tutorialId, DifficultyLevel? level)
    {
        List<QuizQuestion> questions;
        string topic = "Trắc nghiệm tổng hợp .NET";

        if (tutorialId.HasValue)
        {
            var tutorial = await _tutorialService.GetTutorialByIdAsync(tutorialId.Value);
            if (tutorial != null)
            {
                topic = tutorial.Title;
                questions = tutorial.QuizQuestions.ToList();
            }
            else
            {
                questions = await _tutorialService.GetRandomQuizQuestionsAsync(5, level);
            }
        }
        else
        {
            questions = await _tutorialService.GetRandomQuizQuestionsAsync(5, level);
            if (level.HasValue)
            {
                topic = level.Value switch
                {
                    DifficultyLevel.Beginner => "Trắc nghiệm .NET Cơ bản",
                    DifficultyLevel.Intermediate => "Trắc nghiệm .NET Trung cấp & OOP",
                    DifficultyLevel.Advanced => "Trắc nghiệm .NET Nâng cao & Web",
                    _ => topic
                };
            }
        }

        var viewModel = new QuizSubmissionViewModel
        {
            TutorialId = tutorialId,
            TopicTitle = topic,
            Questions = questions.Select(q => new QuizItemViewModel
            {
                QuestionId = q.Id,
                Question = q.Question,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD,
                CorrectOption = q.CorrectOption,
                Explanation = q.Explanation
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Submit(QuizSubmissionViewModel model)
    {
        int score = 0;
        foreach (var q in model.Questions)
        {
            if (!string.IsNullOrEmpty(q.SelectedOption) &&
                q.SelectedOption.Trim().Equals(q.CorrectOption.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }
        }

        model.Score = score;
        model.TotalQuestions = model.Questions.Count;
        model.IsSubmitted = true;

        return View("Take", model);
    }
}

