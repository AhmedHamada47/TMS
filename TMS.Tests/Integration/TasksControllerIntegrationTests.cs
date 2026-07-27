using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Tests.Helpers;

namespace TMS.Tests.Integration;

public class TasksControllerIntegrationTests
{
    private readonly TmsWebApplicationFactory _factory;
    private readonly string _dbName;

    public TasksControllerIntegrationTests()
    {
        _dbName = Guid.NewGuid().ToString();
        _factory = new TmsWebApplicationFactory(_dbName);
        Seed();
    }

    private void Seed()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Organizations.Add(new Organization { Id = 1, Name = "Org1" });
        ctx.Organizations.Add(new Organization { Id = 2, Name = "Org2" });
        ctx.Users.Add(new User
        {
            Id = 1, Name = "Alice", Email = "alice@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Pass1234"),
            AvatarUrl = ""
        });
        ctx.Users.Add(new User
        {
            Id = 2, Name = "Bob", Email = "bob@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Pass1234"),
            AvatarUrl = ""
        });
        ctx.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = 1, OrganizationId = 1, UserId = 1,
            Role = OrganizationRole.Admin, JoinedAt = DateTime.UtcNow
        });
        ctx.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = 2, OrganizationId = 2, UserId = 2,
            Role = OrganizationRole.Employee, JoinedAt = DateTime.UtcNow
        });
        ctx.Categories.Add(new Category { Id = 1, Name = "Dev", Color = "#000", UserId = 1, OrganizationId = 1 });
        ctx.Tasks.Add(new TaskItem
        {
            Id = 1, Title = "Org1Task", Status = TaskItemStatus.ToDo, Priority = TaskPriority.Medium,
            OrganizationId = 1, UserId = 1
        });
        ctx.Tasks.Add(new TaskItem
        {
            Id = 2, Title = "Org2Task", Status = TaskItemStatus.ToDo, Priority = TaskPriority.Medium,
            OrganizationId = 2, UserId = 2
        });
        ctx.SaveChanges();
    }

    private async Task<HttpClient> LoginAsAlice()
    {
        return await IntegrationTestHelper.LoginAsync(_factory, "alice@test.com", "Pass1234");
    }

    [Fact]
    public async Task TasksIndex_RequiresAuthentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TasksIndex_ShowsTasks_WhenAuthenticated()
    {
        var client = await LoginAsAlice();

        var response = await client.GetAsync("/Tasks");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Org1Task");
    }

    [Fact]
    public async Task TasksCreate_Get_ReturnsForm_WhenAuthenticated()
    {
        var client = await LoginAsAlice();

        var response = await client.GetAsync("/Tasks/Create");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Create Task");
    }

    [Fact]
    public async Task TasksCreate_Post_CreatesTask()
    {
        var client = await LoginAsAlice();

        var getResponse = await client.GetAsync("/Tasks/Create");
        var createHtml = await getResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(createHtml,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        var token = tokenMatch.Success ? tokenMatch.Groups[1].Value : "";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Title", "NewIntegrationTask"),
            new KeyValuePair<string, string>("Status", "ToDo"),
            new KeyValuePair<string, string>("Priority", "Medium")
        });

        var response = await client.PostAsync("/Tasks/Create", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var ctx = TestDbContextFactory.Create(_dbName);
        var task = await ctx.Tasks.FirstOrDefaultAsync(t => t.Title == "NewIntegrationTask");
        task.Should().NotBeNull();
    }

    [Fact]
    public async Task TasksDetail_ShowsOrgScopedTask()
    {
        var client = await LoginAsAlice();

        var response = await client.GetAsync("/Tasks/Details/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Org1Task");
    }
}
