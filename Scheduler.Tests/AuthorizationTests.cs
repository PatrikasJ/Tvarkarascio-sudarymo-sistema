using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Scheduler.Tests;

public sealed class AuthorizationTests
{
    [Fact]
    public async Task Viewer_Can_Read_Own_Organization()
    {
        using var factory = new CustomWebAppFactory();
        await factory.InitializeDbAsync();

        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "viewer1@example.com");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Client-Id", "1");
        var response = await client.GetAsync("/schedules");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Generate_Schedule()
    {
        using var factory = new CustomWebAppFactory();
        await factory.InitializeDbAsync();

        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "viewer1@example.com");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Client-Id", "1");
        var response = await client.PostAsJsonAsync("/schedules/generate", 
            new
            {
                PeriodStart = new DateTime(2026, 11, 16),
                PeriodEnd = new DateTime(2026, 11, 23)
            });
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_Cannot_Access_Other_Organization()
    {
        using var factory = new CustomWebAppFactory();
        await factory.InitializeDbAsync();

        using var client = factory.CreateClient();

        var token = await LoginAsync(client, "admin1@example.com");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Client-Id", "2");
        var response = await client.GetAsync("/schedules");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    
    [Fact]
    public async Task Unauthenticated_User_Cannot_Read_Schedules()
    {
        using var factory = new CustomWebAppFactory();
        await factory.InitializeDbAsync();

        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Client-Id", "1");

        var response = await client.GetAsync("/schedules");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    
    
    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/login", 
            new 
            {
                Email = email,
                Password = "password123"
            });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("accessToken").GetString()!;
    }
}