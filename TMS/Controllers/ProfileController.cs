using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TMS.Constants;
using TMS.Data;
using TMS.Models;
using TMS.ViewModels;

namespace TMS.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Displays the current user's profile with task statistics, activity chart data, and completion metrics.
    /// </summary>
    /// <returns>The profile view, or NotFound if the user does not exist.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Index()
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int orgId = int.Parse(User.FindFirstValue(ClaimConstants.OrganizationId)!);
        User? user = await _context.Users.Include(u => u.Tasks.Where(t => t.OrganizationId == orgId)).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        DateTime now = DateTime.UtcNow;
        DateTime thirtyDaysAgo = now.AddDays(-30);

        List<TaskItem> doneTasks = await _context.Tasks
            .Where(t => t.OrganizationId == orgId && t.UserId == userId && t.Status == TaskItemStatus.Done)
            .ToListAsync();

        var dailyCounts = doneTasks
            .GroupBy(t => (t.UpdatedAt ?? t.CreatedAt).Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var dailyData = new List<DailyActivity>();
        for (DateTime d = thirtyDaysAgo.Date; d <= now.Date; d = d.AddDays(1))
        {
            dailyData.Add(new DailyActivity
            {
                Date = d.ToString("yyyy-MM-dd"),
                Count = dailyCounts.GetValueOrDefault(d, 0)
            });
        }

        int streak = 0;
        for (DateTime d = now.Date; d >= thirtyDaysAgo.Date; d = d.AddDays(-1))
        {
            if (dailyCounts.GetValueOrDefault(d, 0) > 0) streak++;
            else break;
        }

        DateTime weekAgo = now.AddDays(-7);
        int tasksThisWeek = doneTasks.Count(t => (t.UpdatedAt ?? t.CreatedAt) >= weekAgo);
        int totalTasks = await _context.Tasks.CountAsync(t => t.OrganizationId == orgId && t.UserId == userId);
        double rate = totalTasks > 0 ? Math.Round((double)doneTasks.Count / totalTasks * 100, 1) : 0;

        ViewBag.ActivityChart = new ActivityChartViewModel
        {
            DailyData = dailyData,
            CurrentStreak = streak,
            TasksCompletedThisWeek = tasksThisWeek,
            CompletionRate = rate
        };

        return View(user);
    }

    /// <summary>
    /// Displays the profile edit form for the current user.
    /// </summary>
    /// <returns>The edit profile view, or NotFound if the user does not exist.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit()
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        return View(user);
    }

    /// <summary>
    /// Handles the update of the current user's profile information.
    /// </summary>
    /// <param name="model">The user model containing updated profile data.</param>
    /// <returns>Redirects to the profile index on success, or returns the edit view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit(User model)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (userId != model.Id) return NotFound();

        ModelState.Remove("Password");

        bool emailTaken = await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId);
        if (emailTaken)
            ModelState.AddModelError("Email", "Email is already in use by another account");

        if (ModelState.IsValid)
        {
            User? user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.Name = model.Name;
            user.Email = model.Email;
            user.AvatarUrl = model.AvatarUrl;

            try
            {
                await _context.SaveChangesAsync();

                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email)
                }, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                TempData["Success"] = "Profile updated successfully!";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Could not update profile. Please try again.";
            }
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Displays the change password form.
    /// </summary>
    /// <returns>The change password view.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ChangePassword()
    {
        return View();
    }

    /// <summary>
    /// Handles the change password request for the current user.
    /// </summary>
    /// <param name="model">The view model containing current and new password.</param>
    /// <returns>Redirects to the profile index on success, or returns the change password view with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password))
        {
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 8 ||
            !model.NewPassword.Any(char.IsLetter) || !model.NewPassword.Any(char.IsDigit))
        {
            ModelState.AddModelError("NewPassword", "Password must be at least 8 characters with at least one letter and one number");
            return View(model);
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Password changed successfully!";
        return RedirectToAction(nameof(Index));
    }
}
