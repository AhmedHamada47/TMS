using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

        var key = email.ToLowerInvariant();
        if (_failedLogins.TryGetValue(key, out var attempt) && attempt.IsLocked)
        {
            ModelState.AddModelError("", "Account temporarily locked due to too many failed attempts. Try again in 15 minutes.");
            return View();
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            var entry = _failedLogins.GetOrAdd(key, _ => new FailedAttempt());
            entry.Count++;
            entry.LastAttempt = DateTime.UtcNow;
            ModelState.AddModelError("", "Invalid email or password");
            return View();
        }

        _failedLogins.TryRemove(key, out _);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string name, string email, string password, string confirmPassword, string? avatarUrl)
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

        if (ModelState.IsValid)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == email);
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
