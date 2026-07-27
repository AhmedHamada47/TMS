using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.ViewModels;

namespace TMS.Services;

/// <summary>Provides implementations for managing categories within an organization.</summary>
public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CategoryService"/> class.</summary>
    /// <param name="context">The database context.</param>
    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Retrieves all categories for the given organization, including their associated tasks.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <returns>A list of categories with their task counts.</returns>
    public async Task<List<Category>> GetCategoriesAsync(int orgId)
    {
        return await _context.Categories
            .Include(c => c.Tasks)
            .Where(c => c.OrganizationId == orgId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>Retrieves a specific category by ID within an organization.</summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <returns>The category if found; otherwise <c>null</c>.</returns>
    public async Task<Category?> GetCategoryAsync(int id, int orgId)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId);
    }

    /// <summary>Retrieves categories formatted for the sidebar display, including task counts, with no-tracking optimization.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <returns>A list of sidebar category view models.</returns>
    public async Task<List<SidebarCategory>> GetSidebarCategoriesAsync(int orgId)
    {
        return await _context.Categories
            .Where(c => c.OrganizationId == orgId)
            .Select(c => new SidebarCategory
            {
                Id = c.Id,
                Name = c.Name,
                Color = c.Color,
                TaskCount = c.Tasks.Count
            })
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>Creates a new category for the specified user and organization.</summary>
    /// <param name="category">The category to create.</param>
    /// <param name="userId">The identifier of the user creating the category.</param>
    /// <param name="orgId">The organization identifier.</param>
    public async Task CreateCategoryAsync(Category category, int userId, int orgId)
    {
        category.UserId = userId;
        category.OrganizationId = orgId;
        category.CreatedAt = DateTime.UtcNow;
        _context.Add(category);
        await _context.SaveChangesAsync();
    }

    /// <summary>Updates an existing category's name, description, and color within an organization.</summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="category">The updated category data.</param>
    /// <param name="orgId">The organization identifier.</param>
    public async Task UpdateCategoryAsync(int id, Category category, int orgId)
    {
        Category? existing = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId);
        if (existing == null) return;

        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.Color = category.Color;
        await _context.SaveChangesAsync();
    }

    /// <summary>Deletes a category within an organization if it exists.</summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    public async Task DeleteCategoryAsync(int id, int orgId)
    {
        Category? category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
