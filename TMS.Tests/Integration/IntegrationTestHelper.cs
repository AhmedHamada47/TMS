using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TMS.Tests.Integration;

public static class IntegrationTestHelper
{
    public static async Task<HttpClient> LoginAsync(WebApplicationFactory<Program> factory, string email, string password)
    {
        var client = factory.CreateClient();

        var getResponse = await client.GetAsync("/Account/Login");
        getResponse.EnsureSuccessStatusCode();
        var html = await getResponse.Content.ReadAsStringAsync();

        var match = Regex.Match(html,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        var token = match.Success ? match.Groups[1].Value : "";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("email", email),
            new KeyValuePair<string, string>("password", password)
        });
        await client.PostAsync("/Account/Login", formData);

        return client;
    }

    public static async Task<(HttpClient Client, string Token)> GetLoginPageWithToken(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();

        var getResponse = await client.GetAsync("/Account/Login");
        getResponse.EnsureSuccessStatusCode();
        var html = await getResponse.Content.ReadAsStringAsync();

        var match = Regex.Match(html,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        var token = match.Success ? match.Groups[1].Value : "";

        return (client, token);
    }
}
