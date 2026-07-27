using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMS.Models;
using TMS.Services;

namespace TMS.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Displays the list of notifications for the current user.
    /// </summary>
    /// <returns>A view with the list of notifications.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Index()
    {
        List<Notification> notifications = await _notificationService.GetNotificationsAsync(CurrentUserId);
        return View(notifications);
    }

    /// <summary>
    /// Marks a single notification as read.
    /// </summary>
    /// <param name="id">The ID of the notification to mark as read.</param>
    /// <returns>HTTP 200 OK on success.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id, CurrentUserId);
        return Ok();
    }

    /// <summary>
    /// Marks all unread notifications as read for the current user.
    /// </summary>
    /// <returns>HTTP 200 OK on success.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsReadAsync(CurrentUserId);
        return Ok();
    }

    /// <summary>
    /// Returns the count of unread notifications for the current user as JSON.
    /// </summary>
    /// <returns>A JSON object containing the unread count.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        int count = await _notificationService.GetUnreadCountAsync(CurrentUserId);
        return Json(new { count });
    }

    /// <summary>
    /// Returns the most recent notifications for the current user as JSON.
    /// </summary>
    /// <returns>A JSON array of recent notifications with id, message, link, isRead, and createdAt fields.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent()
    {
        List<Notification> notifications = await _notificationService.GetRecentAsync(CurrentUserId);
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
