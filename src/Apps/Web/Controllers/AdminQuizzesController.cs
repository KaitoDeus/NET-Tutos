using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Controllers;

[Authorize(Roles = "Admin")]
public class AdminQuizzesController : Controller
{
    private readonly AppDbContext _context;

    public AdminQuizzesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /AdminQuizzes
    public async Task<IActionResult> Index(int? categoryId, int? tutorialId, string? search)
    {
        var query = _context.QuizQuestions
            .Include(q => q.Tutorial)
                .ThenInclude(t => t!.Category)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(q => q.Tutorial != null && q.Tutorial.CategoryId == categoryId.Value);
        }

        if (tutorialId.HasValue)
        {
            query = query.Where(q => q.TutorialId == tutorialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(q => q.Question.Contains(search) || q.Explanation.Contains(search));
        }

        var questions = await query
            .OrderBy(q => q.TutorialId)
            .ThenBy(q => q.Id)
            .ToListAsync();

        ViewBag.Categories = await _context.Categories.OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.Tutorials = await _context.Tutorials.OrderBy(t => t.Title).ToListAsync();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.SelectedTutorial = tutorialId;
        ViewBag.Search = search;

        return View(questions);
    }

    // GET: /AdminQuizzes/Create
    public async Task<IActionResult> Create(int? tutorialId)
    {
        var tutorials = await _context.Tutorials
            .Include(t => t.Category)
            .OrderBy(t => t.Category!.OrderIndex)
            .ThenBy(t => t.OrderIndex)
            .ToListAsync();

        var viewModel = new QuizQuestionEditViewModel
        {
            TutorialId = tutorialId,
            AvailableTutorials = tutorials,
            CorrectOption = "A",
            Difficulty = DifficultyLevel.Beginner
        };

        return View(viewModel);
    }

    // POST: /AdminQuizzes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuizQuestionEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableTutorials = await _context.Tutorials
                .Include(t => t.Category)
                .OrderBy(t => t.Category!.OrderIndex)
                .ThenBy(t => t.OrderIndex)
                .ToListAsync();
            return View(model);
        }

        var question = new QuizQuestion
        {
            TutorialId = model.TutorialId,
            Question = model.Question.Trim(),
            OptionA = model.OptionA.Trim(),
            OptionB = model.OptionB.Trim(),
            OptionC = model.OptionC.Trim(),
            OptionD = model.OptionD.Trim(),
            CorrectOption = model.CorrectOption.ToUpperInvariant(),
            Explanation = model.Explanation?.Trim() ?? string.Empty,
            Difficulty = model.Difficulty
        };

        _context.QuizQuestions.Add(question);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Câu hỏi trắc nghiệm mới đã được thêm thành công!";
        return RedirectToAction(nameof(Index), new { tutorialId = model.TutorialId });
    }

    // GET: /AdminQuizzes/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        var question = await _context.QuizQuestions.FindAsync(id);
        if (question == null) return NotFound();

        var tutorials = await _context.Tutorials
            .Include(t => t.Category)
            .OrderBy(t => t.Category!.OrderIndex)
            .ThenBy(t => t.OrderIndex)
            .ToListAsync();

        var viewModel = new QuizQuestionEditViewModel
        {
            Id = question.Id,
            TutorialId = question.TutorialId,
            Question = question.Question,
            OptionA = question.OptionA,
            OptionB = question.OptionB,
            OptionC = question.OptionC,
            OptionD = question.OptionD,
            CorrectOption = question.CorrectOption,
            Explanation = question.Explanation,
            Difficulty = question.Difficulty,
            AvailableTutorials = tutorials
        };

        return View(viewModel);
    }

    // POST: /AdminQuizzes/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, QuizQuestionEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            model.AvailableTutorials = await _context.Tutorials
                .Include(t => t.Category)
                .OrderBy(t => t.Category!.OrderIndex)
                .ThenBy(t => t.OrderIndex)
                .ToListAsync();
            return View(model);
        }

        var question = await _context.QuizQuestions.FindAsync(id);
        if (question == null) return NotFound();

        question.TutorialId = model.TutorialId;
        question.Question = model.Question.Trim();
        question.OptionA = model.OptionA.Trim();
        question.OptionB = model.OptionB.Trim();
        question.OptionC = model.OptionC.Trim();
        question.OptionD = model.OptionD.Trim();
        question.CorrectOption = model.CorrectOption.ToUpperInvariant();
        question.Explanation = model.Explanation?.Trim() ?? string.Empty;
        question.Difficulty = model.Difficulty;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Câu hỏi đã được cập nhật thành công!";
        return RedirectToAction(nameof(Index), new { tutorialId = model.TutorialId });
    }

    // POST: /AdminQuizzes/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var question = await _context.QuizQuestions.FindAsync(id);
        if (question == null) return NotFound();

        var tutorialId = question.TutorialId;
        _context.QuizQuestions.Remove(question);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã xóa câu hỏi khỏi ngân hàng đề thi.";
        return RedirectToAction(nameof(Index), new { tutorialId });
    }
}
