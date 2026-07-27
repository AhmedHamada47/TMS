using System.Net;
using FluentAssertions;
using TMS.Data;
using TMS.Tests.Helpers;
using TMS.Models;

namespace TMS.Tests.Integration;

public class SmokeTests
{
    private readonly TmsWebApplicationFactory _factory;
    private readonly string _dbName;

    public SmokeTests()
    {
        _dbName = Guid.NewGuid().ToString();
        _factory = new TmsWebApplicationFactory(_dbName);
        Seed();
    }

    private void Seed()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Organizations.Add(new Organization { Id = 1, Name = "Org1" });
        ctx.Users.Add(new User
        {
            Id = 1, Name = "Alice", Email = "alice@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Pass1234"),
            AvatarUrl = ""
        });
        ctx.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = 1, OrganizationId = 1, UserId = 1,
            Role = OrganizationRole.Admin, JoinedAt = DateTime.UtcNow
        });
        ctx.Categories.Add(new Category { Id = 1, Name = "Dev", Color = "#000", UserId = 1, OrganizationId = 1 });
        ctx.Tasks.Add(new TaskItem
        {
            Id = 1, Title = "SmokeTask", Status = TaskItemStatus.ToDo, Priority = TaskPriority.Medium,
            OrganizationId = 1, UserId = 1
        });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task GET_Dashboard_Returns200()
    {
        var client = await LoginAsAlice();
        var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_TasksIndex_Returns200()
    {
        var client = await LoginAsAlice();
        var response = await client.GetAsync("/Tasks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_TasksBoard_Returns200()
    {
        var client = await LoginAsAlice();
        var response = await client.GetAsync("/Tasks/Board");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_TasksCreate_Returns200()
    {
        var client = await LoginAsAlice();
        var response = await client.GetAsync("/Tasks/Create");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_TasksDetail_Returns200()
    {
        var client = await LoginAsAlice();
        var response = await client.GetAsync("/Tasks/Details/1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_TasksIndexWithCategory_Returns200()
    {
        var client = await LoginAsAlice();
        var response = await client.GetAsync("/Tasks?categoryId=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_CategoriesIndex_Returns200()
    {
        var client = await LoginAsAlice();
        var response = await client.GetAsync("/Categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_ReportsIndex_Returns200()
    {
        var client = await LoginAsAlice();
        var response = await client.GetAsync("/Reports");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_ProfileIndex_Returns200()
    {
        var client = await LoginAsAlice();
        var response = await client.GetAsync("/Profile");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> LoginAsAlice()
    {
        return await IntegrationTestHelper.LoginAsync(_factory, "alice@test.com", "Pass1234");
    }
}
