using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TMS.Constants;
using TMS.Data;
using TMS.Models;

namespace TMS.Controllers;

public abstract class BaseController : Controller
{
    /// <summary>
    /// Gets the application database context.
    /// </summary>
    protected AppDbContext Context { get; }

    private int? _orgId;

    protected BaseController(AppDbContext context)
    {
        Context = context;
    }

    /// <summary>
    /// Gets the authenticated user's ID from the NameIdentifier claim.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when the NameIdentifier claim is missing or invalid.</exception>
    protected int CurrentUserId
    {
        get
        {
            string? raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out int id)
                ? throw new System.InvalidOperationException("Authenticated user is missing a valid NameIdentifier claim.")
                : id;
        }
    }

    /// <summary>
    /// Gets the current organization ID from the OrganizationId claim.
    /// Returns 0 if the user is not associated with any organization.
    /// </summary>
    protected int CurrentOrganizationId
    {
        get
        {
            if (_orgId.HasValue) return _orgId.Value;
            string? raw = User.FindFirstValue(ClaimConstants.OrganizationId);
            if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out int id))
            {
                _orgId = id;
                return id;
            }
            return 0;
        }
    }

    /// <summary>
    /// Gets the current user's role within the organization.
    /// </summary>
    protected string CurrentRole => User.FindFirstValue(ClaimConstants.OrganizationRole) ?? "";

    /// <summary>
    /// Gets whether the current user holds a Manager or Admin role.
    /// </summary>
    protected bool IsManagerOrAbove => CurrentRole is "Manager" or "Admin";

    /// <summary>
    /// Gets whether the current user holds a TeamLead, Manager, or Admin role.
    /// </summary>
    protected bool IsTeamLeadOrAbove => CurrentRole is "TeamLead" or "Manager" or "Admin";

    /// <summary>
    /// Ensures the OrganizationId claim is populated before each action executes.
    /// </summary>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await EnsureOrganizationClaimAsync();
        await next();
    }

    private async Task EnsureOrganizationClaimAsync()
    {
        string? raw = User.FindFirstValue(ClaimConstants.OrganizationId);
        if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out int id))
        {
            _orgId = id;
            return;
        }

        OrganizationMembership? membership = await Context.OrganizationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == CurrentUserId);

        if (membership == null) return;

        var claims = User.Claims.ToList();
        claims.RemoveAll(c => c.Type is ClaimConstants.OrganizationId or ClaimConstants.OrganizationRole);
        claims.Add(new Claim(ClaimConstants.OrganizationId, membership.OrganizationId.ToString()));
        claims.Add(new Claim(ClaimConstants.OrganizationRole, membership.Role.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        _orgId = membership.OrganizationId;
    }
}
