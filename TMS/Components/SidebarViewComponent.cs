using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMS.Constants;
using TMS.Services;
using TMS.ViewModels;

namespace TMS.Components;

public class SidebarViewComponent : ViewComponent
{
    private readonly ISidebarService _sidebarService;

    public SidebarViewComponent(ISidebarService sidebarService)
    {
        _sidebarService = sidebarService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        ClaimsPrincipal claimsPrincipal = HttpContext.User as ClaimsPrincipal ?? new ClaimsPrincipal();
        string? userIdClaim = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        string? orgIdClaim = claimsPrincipal.FindFirstValue(ClaimConstants.OrganizationId);
        int userIdParsed = int.TryParse(userIdClaim, out int uid) ? uid : 0;
        int orgIdParsed = int.TryParse(orgIdClaim, out int oid) ? oid : 0;

        string currentController = ViewContext.RouteData.Values["controller"]?.ToString() ?? "";
        string currentAction = ViewContext.RouteData.Values["action"]?.ToString() ?? "";
        string currentCategoryId = ViewContext.HttpContext.Request.Query["categoryId"].ToString();

        SidebarViewModel vm = await _sidebarService.GetSidebarDataAsync(userIdParsed, orgIdParsed, currentController, currentAction, currentCategoryId);

        return View(vm);
    }
}
