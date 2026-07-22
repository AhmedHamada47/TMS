using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.Include(u => u.Tasks).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var doneTasks = await _context.Tasks
            .Where(t => t.UserId == userId && t.Status == TaskItemStatus.Done)
            .ToListAsync();

        var dailyCounts = doneTasks
            .GroupBy(t => (t.UpdatedAt ?? t.CreatedAt).Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var dailyData = new List<DailyActivity>();
        for (var d = thirtyDaysAgo.Date; d <= now.Date; d = d.AddDays(1))
        {
            dailyData.Add(new DailyActivity
            {
                Date = d.ToString("yyyy-MM-dd"),
                Count = dailyCounts.GetValueOrDefault(d, 0)
            });
        }

        var streak = 0;
        for (var d = now.Date; d >= thirtyDaysAgo.Date; d = d.AddDays(-1))
        {
            if (dailyCounts.GetValueOrDefault(d, 0) > 0) streak++;
            else break;
        }

        var weekAgo = now.AddDays(-7);
        var tasksThisWeek = doneTasks.Count(t => (t.UpdatedAt ?? t.CreatedAt) >= weekAgo);
        var totalTasks = await _context.Tasks.CountAsync(t => t.UserId == userId);
        var rate = totalTasks > 0 ? Math.Round((double)doneTasks.Count / totalTasks * 100, 1) : 0;

        ViewBag.ActivityChart = new ActivityChartViewModel
        {
            DailyData = dailyData,
            CurrentStreak = streak,
            TasksCompletedThisWeek = tasksThisWeek,
            CompletionRate = rate
        };

        ViewBag.ChartLabelsJson = System.Text.Json.JsonSerializer.Serialize(dailyData.Select(d => d.Date));
        ViewBag.ChartDataJson = System.Text.Json.JsonSerializer.Serialize(dailyData.Select(d => d.Count));

        return View(user);
    }

    public async Task<IActionResult> Edit()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(User model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (userId != model.Id) return NotFound();

        ModelState.Remove("Password");

        var emailTaken = await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId);
        if (emailTaken)
            ModelState.AddModelError("Email", "Email is already in use by another account");

        if (ModelState.IsValid)
        {
            var user = await _context.Users.FindAsync(userId);
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

    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
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
