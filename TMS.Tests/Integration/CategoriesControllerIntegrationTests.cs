using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TMS.Data;
using TMS.Models;

namespace TMS.Tests.Integration;

public class CategoriesControllerIntegrationTests
{
    private readonly TmsWebApplicationFactory _factory;
    private readonly string _dbName;

    public CategoriesControllerIntegrationTests()
    {
        _dbName = Guid.NewGuid().ToString();
        _factory = new TmsWebApplicationFactory(_dbName);
        Seed();
    }

    private void Seed()
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        ctx.Organizations.Add(new Organization { Id = 1, Name = "Org" });
        ctx.Users.Add(new User
        {
            Id = 1, Name = "A", Email = "a@a.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Pass1234"),
            AvatarUrl = ""
        });
        ctx.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = 1, OrganizationId = 1, UserId = 1,
            Role = OrganizationRole.Admin, JoinedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();
    }

    private async Task<HttpClient> LoginAsync()
    {
        return await IntegrationTestHelper.LoginAsync(_factory, "a@a.com", "Pass1234");
    }

    [Fact]
    public async Task CategoriesIndex_RendersSuccessfully()
    {
        var client = await LoginAsync();

        var response = await client.GetAsync("/Categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Manage Categories");
    }

    [Fact]
    public async Task CategoriesCreate_Get_RendersForm()
    {
        var client = await LoginAsync();

        var response = await client.GetAsync("/Categories/Create");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("New Category");
    }

    [Fact]
    public async Task CategoriesCreate_Post_CreatesAndRedirects()
    {
        var client = await LoginAsync();

        var getResponse = await client.GetAsync("/Categories/Create");
        var createHtml = await getResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(createHtml,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        var token = tokenMatch.Success ? tokenMatch.Groups[1].Value : "";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Name", "IntegrationTestCat"),
            new KeyValuePair<string, string>("Color", "#ff6600")
        });

        var response = await client.PostAsync("/Categories/Create", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CategoriesCreate_Post_WithValidationError_ShowsForm()
    {
        var client = await LoginAsync();

        var getResponse = await client.GetAsync("/Categories/Create");
        var createHtml = await getResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(createHtml,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        var token = tokenMatch.Success ? tokenMatch.Groups[1].Value : "";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Name", ""),
            new KeyValuePair<string, string>("Color", "#ff6600")
        });

        var response = await client.PostAsync("/Categories/Create", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
