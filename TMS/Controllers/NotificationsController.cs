using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TMS.Data;
using TMS.Models;

namespace TMS.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == CurrentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == CurrentUserId);

        if (notification == null) return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _context.Notifications
            .Where(n => n.UserId == CurrentUserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return Ok();
    }

    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _context.Notifications
            .CountAsync(n => n.UserId == CurrentUserId && !n.IsRead);

        return Json(new { count });
    }

    public async Task<IActionResult> GetRecent()
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == CurrentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        return Json(notifications.Select(n => new
        {
            n.Id,
            n.Message,
            n.Link,
            n.IsRead,
            CreatedAt = n.CreatedAt.ToString("g")
        }));
    }
}
