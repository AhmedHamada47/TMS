using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TMS.Constants;
using TMS.Data;
using TMS.Models;

namespace TMS.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, FailedAttempt> _failedLogins = new();

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Displays the login page. Redirects authenticated users to the home page.
    /// </summary>
    /// <returns>The login view or a redirect to the home page if already authenticated.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    /// <summary>
    /// Handles user authentication with email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>Redirects to the home page on success, or returns the login view with an error message on failure.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("Login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "Email is required");
            return View();
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Password is required");
            return View();
        }

        string key = email.ToLowerInvariant();
        if (_failedLogins.TryGetValue(key, out FailedAttempt? attempt) && attempt.IsLocked)
        {
            ModelState.AddModelError("", "Account temporarily locked due to too many failed attempts. Try again in 15 minutes.");
            return View();
        }

        User? user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            FailedAttempt entry = _failedLogins.GetOrAdd(key, _ => new FailedAttempt());
            entry.Count++;
            entry.LastAttempt = DateTime.UtcNow;
            ModelState.AddModelError("", "Invalid email or password");
            return View();
        }

        _failedLogins.TryRemove(key, out _);

        OrganizationMembership? membership = await _context.OrganizationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        if (membership != null)
        {
            claims.Add(new Claim(ClaimConstants.OrganizationId, membership.OrganizationId.ToString()));
            claims.Add(new Claim(ClaimConstants.OrganizationRole, membership.Role.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Displays the registration page. Redirects authenticated users to the home page.
    /// </summary>
    /// <returns>The registration view or a redirect to the home page if already authenticated.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    /// <summary>
    /// Handles new user registration and organization creation or joining.
    /// </summary>
    /// <param name="name">The user's full name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="confirmPassword">Password confirmation.</param>
    /// <param name="avatarUrl">Optional URL for the user's avatar image.</param>
    /// <param name="organizationName">The name of the organization to create or join.</param>
    /// <returns>Redirects to the home page on success, or returns the registration view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("Register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(string name, string email, string password, string confirmPassword, string? avatarUrl, string? organizationName)
    {
        if (string.IsNullOrWhiteSpace(name))
            ModelState.AddModelError("", "Name is required");
        if (string.IsNullOrWhiteSpace(email))
            ModelState.AddModelError("", "Email is required");
        if (string.IsNullOrWhiteSpace(password))
            ModelState.AddModelError("", "Password is required");
        if (password != confirmPassword)
            ModelState.AddModelError("", "Passwords do not match");

        if (!string.IsNullOrWhiteSpace(password) && password.Length < 8)
            ModelState.AddModelError("Password", "Password must be at least 8 characters");
        if (!string.IsNullOrWhiteSpace(password) && !password.Any(char.IsLetter))
            ModelState.AddModelError("Password", "Password must contain at least one letter");
        if (!string.IsNullOrWhiteSpace(password) && !password.Any(char.IsDigit))
            ModelState.AddModelError("Password", "Password must contain at least one number");

        if (string.IsNullOrWhiteSpace(organizationName))
            ModelState.AddModelError("organizationName", "Organization name is required");

        if (ModelState.IsValid)
        {
            bool exists = await _context.Users.AnyAsync(u => u.Email == email);
            if (exists)
            {
                ModelState.AddModelError("", "Email is already registered");
                return View();
            }

            var user = new User
            {
                Name = name,
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                AvatarUrl = avatarUrl ?? "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            Organization? existingOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Name == organizationName);

            OrganizationRole role;
            int orgId;

            if (existingOrg != null)
            {
                orgId = existingOrg.Id;
                role = OrganizationRole.Employee;
            }
            else
            {
                var org = new Organization
                {
                    Name = organizationName!,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Organizations.Add(org);
                await _context.SaveChangesAsync();
                orgId = org.Id;
                role = OrganizationRole.Admin;
            }

            var membership = new OrganizationMembership
            {
                OrganizationId = orgId,
                UserId = user.Id,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };
            _context.OrganizationMemberships.Add(membership);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimConstants.OrganizationId, orgId.ToString()),
                new Claim(ClaimConstants.OrganizationRole, role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            TempData["Success"] = existingOrg != null
                ? $"You've joined the '{organizationName}' organization!"
                : $"Organization '{organizationName}' created successfully!";

            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    /// <summary>
    /// Signs out the current user and redirects to the login page.
    /// </summary>
    /// <returns>A redirect to the login page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}

public class FailedAttempt
{
    public int Count { get; set; }
    public DateTime LastAttempt { get; set; } = DateTime.UtcNow;
    public bool IsLocked => Count >= 5 && DateTime.UtcNow < LastAttempt.AddMinutes(15);
}
