using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Controllers;

[Authorize]
public class CategoriesController : BaseController
{
    public CategoriesController(AppDbContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var categories = await Context.Categories
            .Include(c => c.Tasks)
            .Where(c => c.OrganizationId == CurrentOrganizationId)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (ModelState.IsValid)
        {
            category.UserId = CurrentUserId;
            category.OrganizationId = CurrentOrganizationId;
            category.CreatedAt = DateTime.UtcNow;
            Context.Add(category);
            await Context.SaveChangesAsync();
            TempData["Success"] = "Category created successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var category = await Context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == CurrentOrganizationId);
        if (category == null) return NotFound();

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.Id) return NotFound();

        var existing = await Context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == CurrentOrganizationId);
        if (existing == null) return NotFound();

        if (ModelState.IsValid)
        {
            existing.Name = category.Name;
            existing.Description = category.Description;
            existing.Color = category.Color;

            await Context.SaveChangesAsync();
            TempData["Success"] = "Category updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await Context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == CurrentOrganizationId);
        if (category != null)
        {
            Context.Categories.Remove(category);
            await Context.SaveChangesAsync();
            TempData["Success"] = "Category deleted successfully!";
        }

        return RedirectToAction(nameof(Index));
    }
}
