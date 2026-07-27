using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;
using TMS.Tests.Helpers;

namespace TMS.Tests.Services;

public class ReportServiceTests : IDisposable
{
    private readonly string _dbName;

    public ReportServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        Seed();
    }

    private AppDbContext CreateContext() => TestDbContextFactory.Create(_dbName);

    private void Seed()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Organizations.Add(new Organization { Id = 1, Name = "Org1" });
        ctx.Organizations.Add(new Organization { Id = 2, Name = "Org2" });
        ctx.Users.Add(new User { Id = 1, Name = "Alice", Email = "a@a.com", Password = "pwd" });
        ctx.Users.Add(new User { Id = 2, Name = "Bob", Email = "b@b.com", Password = "pwd" });

        ctx.OrganizationMemberships.Add(new OrganizationMembership
        { Id = 1, OrganizationId = 1, UserId = 1, Role = OrganizationRole.Manager });
        ctx.OrganizationMemberships.Add(new OrganizationMembership
        { Id = 2, OrganizationId = 1, UserId = 2, Role = OrganizationRole.Employee });

        ctx.Teams.Add(new Team { Id = 1, Name = "Engineering", OrganizationId = 1, ManagerUserId = 1 });
        ctx.TeamMemberships.Add(new TeamMembership
        { Id = 1, TeamId = 1, UserId = 1, Role = TeamRole.Lead });

        var now = DateTime.UtcNow;
        ctx.Tasks.Add(new TaskItem
        {
            Id = 1, Title = "CompletedOnTime", Status = TaskItemStatus.Done, Priority = TaskPriority.Medium,
            OrganizationId = 1, UserId = 1, DueDate = now.AddDays(-1), CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-2)
        });
        ctx.Tasks.Add(new TaskItem
        {
            Id = 2, Title = "CompletedLate", Status = TaskItemStatus.Done, Priority = TaskPriority.High,
            OrganizationId = 1, UserId = 1, DueDate = now.AddDays(-4), CreatedAt = now.AddDays(-10),
            UpdatedAt = now.AddDays(-2)
        });
        ctx.Tasks.Add(new TaskItem
        {
            Id = 3, Title = "InProgress", Status = TaskItemStatus.InProgress, Priority = TaskPriority.Urgent,
            OrganizationId = 1, UserId = 1, DueDate = now.AddDays(-1)
        });
        ctx.Tasks.Add(new TaskItem
        {
            Id = 4, Title = "OtherOrg", Status = TaskItemStatus.Done, Priority = TaskPriority.Low,
            OrganizationId = 2, UserId = 2
        });

        ctx.TaskAssignees.Add(new TaskAssignee { Id = 1, TaskItemId = 1, UserId = 1, IsPrimary = true });
        ctx.TaskAssignees.Add(new TaskAssignee { Id = 2, TaskItemId = 2, UserId = 1, IsPrimary = true });
        ctx.TaskAssignees.Add(new TaskAssignee { Id = 3, TaskItemId = 3, UserId = 1, IsPrimary = true });
        ctx.TaskAssignees.Add(new TaskAssignee { Id = 4, TaskItemId = 4, UserId = 2, IsPrimary = true });

        ctx.SaveChanges();
    }

    [Fact]
    public async Task GetTeamReportAsync_CalculatesCompletionRate()
    {
        using var ctx = CreateContext();
        var svc = new ReportService(ctx);

        var report = await svc.GetTeamReportAsync(1);

        report.Employees.Should().ContainSingle(e => e.UserName == "Alice");
        var alice = report.Employees.First(e => e.UserName == "Alice");
        alice.TotalTasks.Should().Be(3);
        alice.CompletedTasks.Should().Be(2);
        alice.CompletionRate.Should().Be(66.7);
    }

    [Fact]
    public async Task GetTeamReportAsync_CalculatesOnTimeRate()
    {
        using var ctx = CreateContext();
        var svc = new ReportService(ctx);

        var report = await svc.GetTeamReportAsync(1);

        var alice = report.Employees.First(e => e.UserName == "Alice");
        alice.OnTimeTasks.Should().Be(1);
        alice.OnTimeRate.Should().Be(50.0);
    }

    [Fact]
    public async Task GetTeamReportAsync_CalculatesCycleHours()
    {
        using var ctx = CreateContext();
        var svc = new ReportService(ctx);

        var report = await svc.GetTeamReportAsync(1);

        var alice = report.Employees.First(e => e.UserName == "Alice");
        alice.AvgCycleHours.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTeamReportAsync_ReturnsTeamSummary()
    {
        using var ctx = CreateContext();
        var svc = new ReportService(ctx);

        var report = await svc.GetTeamReportAsync(1);

        report.TeamSummary.TeamName.Should().Be("Engineering");
        report.TeamSummary.TotalTasks.Should().Be(3);
        report.TeamSummary.CompletedTasks.Should().Be(2);
    }

    [Fact]
    public async Task GetTeamReportAsync_ScopesByOrg()
    {
        using var ctx = CreateContext();
        var svc = new ReportService(ctx);

        var report = await svc.GetTeamReportAsync(1);

        report.Employees.Any(e => e.UserName == "Bob").Should().BeFalse();
    }

    [Fact]
    public async Task GetTeamReportAsync_ReturnsEmpty_WhenNoTeam()
    {
        using var ctx = CreateContext();
        var svc = new ReportService(ctx);

        var report = await svc.GetTeamReportAsync(99);

        report.TeamSummary.TeamName.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetEmployeeDetailAsync_ReturnsEmployeeTasks()
    {
        using var ctx = CreateContext();
        var svc = new ReportService(ctx);

        var (tasks, name, avatar) = await svc.GetEmployeeDetailAsync(1, 1);

        tasks.Should().HaveCount(3);
        name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetEmployeeDetailAsync_ReturnsEmpty_ForOtherOrg()
    {
        using var ctx = CreateContext();
        var svc = new ReportService(ctx);

        var (tasks, name, _) = await svc.GetEmployeeDetailAsync(2, 2);

        tasks.Should().BeEmpty();
        name.Should().BeEmpty();
    }

    public void Dispose()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Database.EnsureDeleted();
    }
}
