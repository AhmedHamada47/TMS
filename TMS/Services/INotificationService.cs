using TMS.Models;

namespace TMS.Services;

/// <summary>Defines methods for managing user notifications.</summary>
public interface INotificationService
{
    /// <summary>Creates a new notification for a user.</summary>
    /// <param name="userId">The identifier of the target user.</param>
    /// <param name="message">The notification message text.</param>
    /// <param name="link">An optional deep-link URL associated with the notification.</param>
    Task CreateNotificationAsync(int userId, string message, string? link = null);
    /// <summary>Retrieves all notifications for a user, ordered by most recent first.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of notifications.</returns>
    Task<List<Notification>> GetNotificationsAsync(int userId);
    /// <summary>Marks a specific notification as read.</summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="userId">The user identifier.</param>
    Task MarkAsReadAsync(int id, int userId);
    /// <summary>Marks all unread notifications as read for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    Task MarkAllAsReadAsync(int userId);
    /// <summary>Returns the count of unread notifications for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The number of unread notifications.</returns>
    Task<int> GetUnreadCountAsync(int userId);
    /// <summary>Retrieves the ten most recent notifications for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of recent notifications.</returns>
    Task<List<Notification>> GetRecentAsync(int userId);
}
