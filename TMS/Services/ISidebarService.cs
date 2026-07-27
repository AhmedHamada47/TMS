using TMS.ViewModels;

namespace TMS.Services;

/// <summary>Defines methods for building the sidebar user interface data.</summary>
public interface ISidebarService
{
    /// <summary>Builds the sidebar view model for the specified user and organization.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="currentController">The name of the current controller for active-state highlighting.</param>
    /// <param name="currentAction">The name of the current action for active-state highlighting.</param>
    /// <param name="currentCategoryId">The currently selected category identifier, if any.</param>
    /// <returns>A view model containing user info, organization info, and categories.</returns>
    Task<SidebarViewModel> GetSidebarDataAsync(int userId, int orgId, string currentController, string currentAction, string currentCategoryId);
}
