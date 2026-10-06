using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Controllers;

[Authorize(Roles = "Admin")]
public class AdminTutorialsController : Controller
{
    private readonly AppDbContext _context;

    public AdminTutorialsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /AdminTutorials
    public async Task<IActionResult> Index(int? categoryId, string? search)
    {
        var query = _context.Tutorials
            .Include(t => t.Category)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Title.Contains(search) || t.Slug.Contains(search));
        }

        var tutorials = await query
            .OrderBy(t => t.CategoryId)
            .ThenBy(t => t.OrderIndex)
            .ToListAsync();

        ViewBag.Categories = await _context.Categories.OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.Search = search;

        return View(tutorials);
    }

    // GET: /AdminTutorials/Create
    public async Task<IActionResult> Create()
    {
        var categories = await _context.Categories.OrderBy(c => c.OrderIndex).ToListAsync();
        var viewModel = new TutorialEditViewModel
        {
            AvailableCategories = categories,
            OrderIndex = await _context.Tutorials.CountAsync() + 1
        };
        return View(viewModel);
    }

    // POST: /AdminTutorials/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TutorialEditViewModel model)
    {
        if (await _context.Tutorials.AnyAsync(t => t.Slug == model.Slug))
        {
            ModelState.AddModelError("Slug", "Đường dẫn Slug này đã tồn tại, vui lòng chọn Slug khác.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _context.Categories.OrderBy(c => c.OrderIndex).ToListAsync();
            return View(model);
        }

        var tutorial = new Tutorial
        {
            Title = model.Title,
            Slug = model.Slug.Trim().ToLowerInvariant(),
            Summary = model.Summary,
            ContentMarkdown = model.ContentMarkdown,
            CategoryId = model.CategoryId,
            Difficulty = model.Difficulty,
            EstimatedReadingMinutes = model.EstimatedReadingMinutes,
            OrderIndex = model.OrderIndex,
            IsFeatured = model.IsFeatured,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tutorials.Add(tutorial);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Bài học '{tutorial.Title}' đã được tạo thành công!";
        return RedirectToAction(nameof(Index));
    }

    // GET: /AdminTutorials/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        var tutorial = await _context.Tutorials.FindAsync(id);
        if (tutorial == null) return NotFound();

        var categories = await _context.Categories.OrderBy(c => c.OrderIndex).ToListAsync();
        var viewModel = new TutorialEditViewModel
        {
            Id = tutorial.Id,
            Title = tutorial.Title,
            Slug = tutorial.Slug,
            Summary = tutorial.Summary,
            ContentMarkdown = tutorial.ContentMarkdown,
            CategoryId = tutorial.CategoryId,
            Difficulty = tutorial.Difficulty,
            EstimatedReadingMinutes = tutorial.EstimatedReadingMinutes,
            OrderIndex = tutorial.OrderIndex,
            IsFeatured = tutorial.IsFeatured,
            AvailableCategories = categories
        };

        return View(viewModel);
    }

    // POST: /AdminTutorials/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TutorialEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (await _context.Tutorials.AnyAsync(t => t.Slug == model.Slug && t.Id != id))
        {
            ModelState.AddModelError("Slug", "Đường dẫn Slug này đã được sử dụng bởi bài học khác.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _context.Categories.OrderBy(c => c.OrderIndex).ToListAsync();
            return View(model);
        }

        var tutorial = await _context.Tutorials.FindAsync(id);
        if (tutorial == null) return NotFound();

        tutorial.Title = model.Title;
        tutorial.Slug = model.Slug.Trim().ToLowerInvariant();
        tutorial.Summary = model.Summary;
        tutorial.ContentMarkdown = model.ContentMarkdown;
        tutorial.CategoryId = model.CategoryId;
        tutorial.Difficulty = model.Difficulty;
        tutorial.EstimatedReadingMinutes = model.EstimatedReadingMinutes;
        tutorial.OrderIndex = model.OrderIndex;
        tutorial.IsFeatured = model.IsFeatured;
        tutorial.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Bài học '{tutorial.Title}' đã được cập nhật thành công!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /AdminTutorials/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var tutorial = await _context.Tutorials.FindAsync(id);
        if (tutorial == null) return NotFound();

        _context.Tutorials.Remove(tutorial);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã xóa bài học '{tutorial.Title}'.";
        return RedirectToAction(nameof(Index));
    }
}
