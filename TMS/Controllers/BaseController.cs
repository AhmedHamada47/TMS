using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TMS.Data;

namespace TMS.Controllers;

public abstract class BaseController : Controller
{
    protected AppDbContext Context { get; }

    private int? _orgId;

    protected BaseController(AppDbContext context)
    {
        Context = context;
    }

    protected int CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var id)
                ? throw new System.InvalidOperationException("Authenticated user is missing a valid NameIdentifier claim.")
                : id;
        }
    }

    protected int CurrentOrganizationId
    {
        get
        {
            if (_orgId.HasValue) return _orgId.Value;
            var raw = User.FindFirstValue("OrganizationId");
            if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var id))
            {
                _orgId = id;
                return id;
            }
            return 0;
        }
    }

    protected string CurrentRole => User.FindFirstValue("OrganizationRole") ?? "";

    protected bool IsManagerOrAbove => CurrentRole is "Manager" or "Admin";

    protected bool IsTeamLeadOrAbove => CurrentRole is "TeamLead" or "Manager" or "Admin";

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await EnsureOrganizationClaimAsync();
        await next();
    }

    private async Task EnsureOrganizationClaimAsync()
    {
        var raw = User.FindFirstValue("OrganizationId");
        if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var id))
        {
            _orgId = id;
            return;
        }

        var membership = await Context.OrganizationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == CurrentUserId);

        if (membership == null) return;

        var claims = User.Claims.ToList();
        claims.RemoveAll(c => c.Type is "OrganizationId" or "OrganizationRole");
        claims.Add(new Claim("OrganizationId", membership.OrganizationId.ToString()));
        claims.Add(new Claim("OrganizationRole", membership.Role.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        _orgId = membership.OrganizationId;
    }
}
