using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;
using TMS.Tests.Helpers;

namespace TMS.Tests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly string _dbName;

    public NotificationServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        Seed();
    }

    private AppDbContext CreateContext() => TestDbContextFactory.Create(_dbName);

    private void Seed()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Users.Add(new User { Id = 1, Name = "Alice", Email = "a@a.com", Password = "pwd" });
        ctx.Users.Add(new User { Id = 2, Name = "Bob", Email = "b@b.com", Password = "pwd" });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task CreateNotificationAsync_CreatesForCorrectUser()
    {
        using var ctx = CreateContext();
        var svc = new NotificationService(ctx);

        await svc.CreateNotificationAsync(2, "Bob was assigned a task", "/Tasks/Details/1");

        var notif = await ctx.Notifications.FirstOrDefaultAsync(n => n.UserId == 2);
        notif.Should().NotBeNull();
        notif!.Message.Should().Be("Bob was assigned a task");
        notif.Link.Should().Be("/Tasks/Details/1");
        notif.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task CreateNotificationAsync_DoesNotCreateForWrongUser()
    {
        using var ctx = CreateContext();
        var svc = new NotificationService(ctx);

        await svc.CreateNotificationAsync(1, "For Alice", null);

        var bobNotif = await ctx.Notifications.FirstOrDefaultAsync(n => n.UserId == 2);
        bobNotif.Should().BeNull();
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        using var ctx = CreateContext();
        var svc = new NotificationService(ctx);

        await svc.CreateNotificationAsync(1, "Notif1", null);
        await svc.CreateNotificationAsync(1, "Notif2", null);

        var count = await svc.GetUnreadCountAsync(1);
        count.Should().Be(2);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksNotificationRead()
    {
        using var ctx = CreateContext();
        var svc = new NotificationService(ctx);

        await svc.CreateNotificationAsync(1, "Test", null);
        var notif = await ctx.Notifications.FirstAsync(n => n.UserId == 1);

        await svc.MarkAsReadAsync(notif.Id, 1);

        var updated = await ctx.Notifications.FindAsync(notif.Id);
        updated!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllRead()
    {
        using var ctx = CreateContext();
        var svc = new NotificationService(ctx);

        await svc.CreateNotificationAsync(1, "A", null);
        await svc.CreateNotificationAsync(1, "B", null);

        await svc.MarkAllAsReadAsync(1);

        var unread = await ctx.Notifications.CountAsync(n => n.UserId == 1 && !n.IsRead);
        unread.Should().Be(0);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsLatestNotifications()
    {
        using var ctx = CreateContext();
        var svc = new NotificationService(ctx);

        for (int i = 0; i < 15; i++)
            await svc.CreateNotificationAsync(1, $"Notif{i}", null);

        var recent = await svc.GetRecentAsync(1);

        recent.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetNotificationsAsync_ReturnsAllForUser()
    {
        using var ctx = CreateContext();
        var svc = new NotificationService(ctx);

        await svc.CreateNotificationAsync(1, "A", null);
        await svc.CreateNotificationAsync(2, "B", null);

        var notifs = await svc.GetNotificationsAsync(1);
        notifs.Should().ContainSingle(n => n.Message == "A");
        notifs.Should().NotContain(n => n.Message == "B");
    }

    public void Dispose()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Database.EnsureDeleted();
    }
}
