using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TMS.Data;
using TMS.ViewModels;

namespace TMS.Components;

public class SidebarViewComponent : ViewComponent
{
    private readonly AppDbContext _context;

    public SidebarViewComponent(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var claimsPrincipal = HttpContext.User as System.Security.Claims.ClaimsPrincipal ?? new System.Security.Claims.ClaimsPrincipal();
        var userIdClaim = claimsPrincipal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        var userIdParsed = int.TryParse(userIdClaim, out var uid) ? uid : 0;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userIdParsed);

        var categories = await _context.Categories
            .Where(c => c.UserId == userIdParsed)
            .Select(c => new SidebarCategory
            {
                Id = c.Id,
                Name = c.Name,
                Color = c.Color,
                TaskCount = c.Tasks.Count
            })
            .AsNoTracking()
            .ToListAsync();

        var vm = new SidebarViewModel
        {
            UserName = claimsPrincipal.FindFirstValue(System.Security.Claims.ClaimTypes.Name) ?? "User",
            UserEmail = claimsPrincipal.FindFirstValue(System.Security.Claims.ClaimTypes.Email) ?? "",
            AvatarUrl = user?.AvatarUrl ?? "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png",
            Categories = categories,
            CurrentController = ViewContext.RouteData.Values["controller"]?.ToString() ?? "",
            CurrentAction = ViewContext.RouteData.Values["action"]?.ToString() ?? "",
            CurrentCategoryId = ViewContext.HttpContext.Request.Query["categoryId"].ToString()
        };

        return View(vm);
    }
}
