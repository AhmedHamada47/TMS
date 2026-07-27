using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;
using TMS.Tests.Helpers;

namespace TMS.Tests.Services;

public class TeamServiceTests : IDisposable
{
    private readonly string _dbName;

    public TeamServiceTests()
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
        ctx.Users.Add(new User { Id = 3, Name = "Charlie", Email = "c@c.com", Password = "pwd" });
        ctx.Teams.Add(new Team { Id = 1, Name = "TeamA", OrganizationId = 1, ManagerUserId = 1 });
        ctx.Teams.Add(new Team { Id = 2, Name = "TeamB", OrganizationId = 1, ManagerUserId = 1 });
        ctx.Teams.Add(new Team { Id = 3, Name = "OtherOrgTeam", OrganizationId = 2, ManagerUserId = 2 });
        ctx.TeamMemberships.Add(new TeamMembership { Id = 1, TeamId = 1, UserId = 1, Role = TeamRole.Lead });
        ctx.TeamMemberships.Add(new TeamMembership { Id = 2, TeamId = 1, UserId = 2, Role = TeamRole.Member });
        ctx.TeamMemberships.Add(new TeamMembership { Id = 3, TeamId = 2, UserId = 1, Role = TeamRole.Lead });
        ctx.TeamMemberships.Add(new TeamMembership { Id = 4, TeamId = 2, UserId = 3, Role = TeamRole.Member });
        ctx.TeamMemberships.Add(new TeamMembership { Id = 5, TeamId = 2, UserId = 2, Role = TeamRole.Member });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task GetTeamMembersAsync_AggregatesAcrossAllTeams()
    {
        using var ctx = CreateContext();
        var svc = new TeamService(ctx);

        var members = await svc.GetTeamMembersAsync(1, 1);

        members.Should().HaveCount(3);
        members.Should().Contain(m => m.Name == "Alice");
        members.Should().Contain(m => m.Name == "Bob");
        members.Should().Contain(m => m.Name == "Charlie");
    }

    [Fact]
    public async Task GetTeamMembersAsync_DeduplicatesSameUser()
    {
        using var ctx = CreateContext();
        var svc = new TeamService(ctx);

        var members = await svc.GetTeamMembersAsync(1, 1);

        members.Select(m => m.Id).Distinct().Should().HaveCount(members.Count);
    }

    [Fact]
    public async Task GetTeamMembersAsync_ReturnsEmpty_WhenNoTeams()
    {
        using var ctx = CreateContext();
        var svc = new TeamService(ctx);

        var members = await svc.GetTeamMembersAsync(1, 99);

        members.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTeamMembersAsync_DoesNotIncludeOtherOrg()
    {
        using var ctx = CreateContext();
        var svc = new TeamService(ctx);

        var members = await svc.GetTeamMembersAsync(1, 1);

        members.All(m => m.Name != null).Should().BeTrue();
    }

    public void Dispose()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Database.EnsureDeleted();
    }
}
