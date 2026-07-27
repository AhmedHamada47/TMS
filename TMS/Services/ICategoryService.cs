using TMS.Models;
using TMS.ViewModels;

namespace TMS.Services;

/// <summary>Defines methods for managing categories within an organization.</summary>
public interface ICategoryService
{
    /// <summary>Retrieves all categories for the given organization, including their associated tasks.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <returns>A list of categories with their task counts.</returns>
    Task<List<Category>> GetCategoriesAsync(int orgId);
    /// <summary>Retrieves a specific category by ID within an organization.</summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <returns>The category if found; otherwise <c>null</c>.</returns>
    Task<Category?> GetCategoryAsync(int id, int orgId);
    /// <summary>Retrieves categories formatted for the sidebar display, including task counts.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <returns>A list of sidebar category view models.</returns>
    Task<List<SidebarCategory>> GetSidebarCategoriesAsync(int orgId);
    /// <summary>Creates a new category for the specified user and organization.</summary>
    /// <param name="category">The category to create.</param>
    /// <param name="userId">The identifier of the user creating the category.</param>
    /// <param name="orgId">The organization identifier.</param>
    Task CreateCategoryAsync(Category category, int userId, int orgId);
    /// <summary>Updates an existing category within an organization.</summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="category">The updated category data.</param>
    /// <param name="orgId">The organization identifier.</param>
    Task UpdateCategoryAsync(int id, Category category, int orgId);
    /// <summary>Deletes a category within an organization.</summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    Task DeleteCategoryAsync(int id, int orgId);
}
