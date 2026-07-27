using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;
using TMS.Tests.Helpers;
using TMS.ViewModels;

namespace TMS.Tests.Services;

public class TaskServiceTests : IDisposable
{
    private readonly string _dbName;

    public TaskServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        Seed();
    }

    private AppDbContext CreateContext() => TestDbContextFactory.Create(_dbName);

    private void Seed()
    {
        using var ctx = CreateContext();
        ctx.Organizations.Add(new Organization { Id = 1, Name = "Org1" });
        ctx.Organizations.Add(new Organization { Id = 2, Name = "Org2" });
        ctx.Users.Add(new User { Id = 1, Name = "Alice", Email = "a@a.com", Password = "pwd" });
        ctx.Users.Add(new User { Id = 2, Name = "Bob", Email = "b@b.com", Password = "pwd" });
        ctx.Categories.Add(new Category { Id = 1, Name = "Dev", Color = "#000", UserId = 1, OrganizationId = 1 });
        ctx.Categories.Add(new Category { Id = 2, Name = "Design", Color = "#fff", UserId = 1, OrganizationId = 1 });
        ctx.Tasks.Add(new TaskItem
        {
            Id = 1, Title = "Task1", Status = TaskItemStatus.ToDo, Priority = TaskPriority.High,
            OrganizationId = 1, UserId = 1
        });
        ctx.Tasks.Add(new TaskItem
        {
            Id = 2, Title = "Org2Task", Status = TaskItemStatus.ToDo, Priority = TaskPriority.Medium,
            OrganizationId = 2, UserId = 2
        });
        ctx.SaveChanges();
    }

    private TaskService CreateService(AppDbContext ctx)
    {
        var teamService = new TeamService(ctx);
        var notifService = new NotificationService(ctx);
        return new TaskService(ctx, teamService, notifService);
    }

    [Fact]
    public async Task GetTaskDetailsAsync_ReturnsTask_WhenSameOrg()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);

        var result = await svc.GetTaskDetailsAsync(1, 1, 1, true);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Task1");
    }

    [Fact]
    public async Task GetTaskDetailsAsync_ReturnsNull_WhenDifferentOrg()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);

        var result = await svc.GetTaskDetailsAsync(1, 2, 2, true);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateTaskAsync_CreatesTaskAndLogsActivity()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var vm = new TaskViewModel { Title = "NewTask", Status = TaskItemStatus.ToDo, Priority = TaskPriority.Medium };

        await svc.CreateTaskAsync(vm, 1, 1, true);

        var task = await ctx.Tasks.FirstOrDefaultAsync(t => t.Title == "NewTask");
        task.Should().NotBeNull();
        task!.OrganizationId.Should().Be(1);
        var log = await ctx.TaskActivityLogs.FirstOrDefaultAsync(l => l.TaskItemId == task.Id);
        log.Should().NotBeNull();
        log!.FieldName.Should().Be("Task");
    }

    [Fact]
    public async Task UpdateTaskAsync_UpdatesFieldsAndLogsChanges()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var vm = new TaskViewModel
        {
            Id = 1,
            Title = "UpdatedTitle",
            Description = "UpdatedDesc",
            Status = TaskItemStatus.InProgress,
            Priority = TaskPriority.Urgent,
            DueDate = DateTime.UtcNow.AddDays(1),
            CategoryId = 2
        };

        await svc.UpdateTaskAsync(1, vm, 1, 1, true);

        var task = await ctx.Tasks.FindAsync(1);
        task!.Title.Should().Be("UpdatedTitle");
        task.Status.Should().Be(TaskItemStatus.InProgress);
        var logs = await ctx.TaskActivityLogs.Where(l => l.TaskItemId == 1).ToListAsync();
        logs.Should().Contain(l => l.FieldName == "Title" && l.OldValue == "Task1" && l.NewValue == "UpdatedTitle");
        logs.Should().Contain(l => l.FieldName == "Priority" && l.OldValue == "High" && l.NewValue == "Urgent");
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesStatusAndLogs()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);

        await svc.UpdateStatusAsync(1, TaskItemStatus.Done, 1, 1, true);

        var task = await ctx.Tasks.FindAsync(1);
        task!.Status.Should().Be(TaskItemStatus.Done);
        var log = await ctx.TaskActivityLogs
            .FirstOrDefaultAsync(l => l.TaskItemId == 1 && l.FieldName == "Status");
        log.Should().NotBeNull();
        log!.OldValue.Should().Be("ToDo");
        log.NewValue.Should().Be("Done");
    }

    [Fact]
    public async Task DeleteTaskAsync_RemovesTask_WhenAuthorized()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);

        await svc.DeleteTaskAsync(1, 1, 1, true);

        var task = await ctx.Tasks.FindAsync(1);
        task.Should().BeNull();
    }

    [Fact]
    public async Task OrgScoping_Delete_DoesNotDeleteTaskFromDifferentOrg()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);

        await svc.DeleteTaskAsync(1, 2, 2, true);

        var task = await ctx.Tasks.FindAsync(1);
        task.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFilteredTasksAsync_ScopesByOrg()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);

        var result = await svc.GetFilteredTasksAsync(1, 1, true, null, null, null, null, 1, 10);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(t => t.Title == "Task1");
    }

    [Fact]
    public async Task AddCommentAsync_CreatesComment()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);

        await svc.AddCommentAsync(1, "Test comment", null, 1, 1, true, "Alice");

        var comment = await ctx.TaskComments.FirstOrDefaultAsync(c => c.TaskItemId == 1);
        comment.Should().NotBeNull();
        comment!.Content.Should().Be("Test comment");
    }

    public void Dispose()
    {
        using var ctx = CreateContext();
        ctx.Database.EnsureDeleted();
    }
}
