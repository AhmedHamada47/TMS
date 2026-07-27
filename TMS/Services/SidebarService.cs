using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TMS.Constants;
using TMS.Data;
using TMS.Models;
using TMS.ViewModels;

namespace TMS.Services;

/// <summary>Provides implementations for building the sidebar user interface data.</summary>
public class SidebarService : ISidebarService
{
    private readonly AppDbContext _context;
    private readonly ICategoryService _categoryService;

    /// <summary>Initializes a new instance of the <see cref="SidebarService"/> class.</summary>
    /// <param name="context">The database context.</param>
    /// <param name="categoryService">The category service for retrieving sidebar categories.</param>
    public SidebarService(AppDbContext context, ICategoryService categoryService)
    {
        _context = context;
        _categoryService = categoryService;
    }

    /// <summary>Builds the sidebar view model for the specified user and organization.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="currentController">The name of the current controller for active-state highlighting.</param>
    /// <param name="currentAction">The name of the current action for active-state highlighting.</param>
    /// <param name="currentCategoryId">The currently selected category identifier, if any.</param>
    /// <returns>A view model containing user info, organization info, and categories.</returns>
    public async Task<SidebarViewModel> GetSidebarDataAsync(int userId, int orgId, string currentController, string currentAction, string currentCategoryId)
    {
        User? user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        OrganizationMembership? membership = await _context.OrganizationMemberships
            .Include(m => m.Organization)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId);

        List<SidebarCategory> categories = await _categoryService.GetSidebarCategoriesAsync(orgId);

        return new SidebarViewModel
        {
            UserName = user?.Name ?? "User",
            UserEmail = user?.Email ?? "",
            AvatarUrl = user?.AvatarUrl ?? "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png",
            OrganizationName = membership?.Organization?.Name ?? "My Organization",
            OrganizationRole = membership?.Role.ToString() ?? "",
            Categories = categories,
            CurrentController = currentController,
            CurrentAction = currentAction,
            CurrentCategoryId = currentCategoryId
        };
    }
}
