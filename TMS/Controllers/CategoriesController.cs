using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS.Data;
using TMS.Models;
using TMS.Services;

namespace TMS.Controllers;

[Authorize]
public class CategoriesController : BaseController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(AppDbContext context, ICategoryService categoryService) : base(context)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Displays a list of all categories for the current organization.
    /// </summary>
    /// <returns>A view with the list of categories.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Index()
    {
        List<Category> categories = await _categoryService.GetCategoriesAsync(CurrentOrganizationId);
        return View(categories);
    }

    /// <summary>
    /// Displays the category creation form.
    /// </summary>
    /// <returns>The create category view.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Handles the creation of a new category.
    /// </summary>
    /// <param name="category">The category model containing the new category data.</param>
    /// <returns>Redirects to the index on success, or returns the create view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Create(Category category)
    {
        if (ModelState.IsValid)
        {
            await _categoryService.CreateCategoryAsync(category, CurrentUserId, CurrentOrganizationId);
            TempData["Success"] = "Category created successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    /// <summary>
    /// Displays the category edit form for the specified category.
    /// </summary>
    /// <param name="id">The ID of the category to edit.</param>
    /// <returns>The edit category view, or NotFound if the category does not exist.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        Category? category = await _categoryService.GetCategoryAsync(id.Value, CurrentOrganizationId);
        if (category == null) return NotFound();

        return View(category);
    }

    /// <summary>
    /// Handles the update of an existing category.
    /// </summary>
    /// <param name="id">The ID of the category to update.</param>
    /// <param name="category">The category model with updated data.</param>
    /// <returns>Redirects to the index on success, returns the edit view with validation errors, or NotFound if IDs do not match.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.Id) return NotFound();

        if (ModelState.IsValid)
        {
            await _categoryService.UpdateCategoryAsync(id, category, CurrentOrganizationId);
            TempData["Success"] = "Category updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    /// <summary>
    /// Handles the deletion of a category.
    /// </summary>
    /// <param name="id">The ID of the category to delete.</param>
    /// <returns>Redirects to the index after deletion.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Delete(int id)
    {
        await _categoryService.DeleteCategoryAsync(id, CurrentOrganizationId);
        TempData["Success"] = "Category deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}
