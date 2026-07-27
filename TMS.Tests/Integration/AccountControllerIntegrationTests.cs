using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TMS.Data;
using TMS.Models;
using TMS.Tests.Helpers;

namespace TMS.Tests.Integration;

public class AccountControllerIntegrationTests
{
    private readonly TmsWebApplicationFactory _factory;
    private readonly string _dbName;

    public AccountControllerIntegrationTests()
    {
        _dbName = Guid.NewGuid().ToString();
        _factory = new TmsWebApplicationFactory(_dbName);
        Seed();
    }

    private void Seed()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Organizations.Add(new Organization { Id = 1, Name = "TestOrg" });
        ctx.Users.Add(new User
        {
            Id = 1, Name = "TestUser", Email = "test@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Password1"),
            AvatarUrl = ""
        });
        ctx.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = 1, OrganizationId = 1, UserId = 1,
            Role = OrganizationRole.Admin, JoinedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Login_Get_ReturnsLoginPage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Sign In");
    }

    [Fact]
    public async Task Login_Post_WithValidCredentials_RedirectsToHome()
    {
        var (client, token) = await GetLoginPageWithToken();
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("email", "test@test.com"),
            new KeyValuePair<string, string>("password", "Password1")
        });

        var response = await client.PostAsync("/Account/Login", formData);
        var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? "";
        var html = await response.Content.ReadAsStringAsync();

        finalUrl.Should().NotContain("Login");
        html.Should().Contain("Dashboard");
    }

    [Fact]
    public async Task Login_Post_WithInvalidPassword_ReturnsViewWithError()
    {
        var (client, token) = await GetLoginPageWithToken();
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("email", "test@test.com"),
            new KeyValuePair<string, string>("password", "WrongPassword1")
        });

        var response = await client.PostAsync("/Account/Login", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_Post_WithEmptyEmail_ReturnsError()
    {
        var (client, token) = await GetLoginPageWithToken();
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("email", ""),
            new KeyValuePair<string, string>("password", "Password1")
        });

        var response = await client.PostAsync("/Account/Login", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_RedirectsToHome_WhenAlreadyAuthenticated()
    {
        var client = await IntegrationTestHelper.LoginAsync(_factory, "test@test.com", "Password1");

        var response = await client.GetAsync("/Account/Login");
        var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? "";
        var html = await response.Content.ReadAsStringAsync();

        finalUrl.Should().NotContain("Login");
        html.Should().Contain("Dashboard");
    }

    private async Task<(HttpClient Client, string Token)> GetLoginPageWithToken()
    {
        return await IntegrationTestHelper.GetLoginPageWithToken(_factory);
    }
}
