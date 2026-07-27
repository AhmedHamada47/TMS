using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

/// <summary>Provides implementations for managing user notifications.</summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="NotificationService"/> class.</summary>
    /// <param name="context">The database context.</param>
    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Creates a new notification for a user with the given message and optional link.</summary>
    /// <param name="userId">The identifier of the target user.</param>
    /// <param name="message">The notification message text.</param>
    /// <param name="link">An optional deep-link URL associated with the notification.</param>
    public async Task CreateNotificationAsync(int userId, string message, string? link = null)
    {
        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            Link = link,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    /// <summary>Retrieves all notifications for a user, ordered by most recent first.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of notifications ordered by creation date descending.</returns>
    public async Task<List<Notification>> GetNotificationsAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Marks a specific notification as read for the given user.</summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="userId">The user identifier.</param>
    public async Task MarkAsReadAsync(int id, int userId)
    {
        Notification? notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification == null) return;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
    }

    /// <summary>Marks all unread notifications as read for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    public async Task MarkAllAsReadAsync(int userId)
    {
        List<Notification> unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (Notification? n in unread)
            n.IsRead = true;
        await _context.SaveChangesAsync();
    }

    /// <summary>Returns the count of unread notifications for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The number of unread notifications.</returns>
    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    /// <summary>Retrieves the ten most recent notifications for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of recent notifications ordered by creation date descending.</returns>
    public async Task<List<Notification>> GetRecentAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();
    }
}
