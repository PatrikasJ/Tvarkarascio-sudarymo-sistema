using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scheduler.Api.Infrastructure;

namespace Scheduler.Tests;

public class ScheduleApiIntegrationTests
{
    [Fact]
    public async Task GenerateSchedule_ReturnsSuccessForClient1()
    {
        using var factory = new CustomWebAppFactory();
        await factory.InitializeDbAsync();
        
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Id", "1");

        var request = new
        {
            PeriodStart = new DateTime(2026, 11, 16),
            PeriodEnd = new DateTime(2026, 11, 17),
        };
        var response = await client.PostAsJsonAsync("/schedules/generate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task GenerateSchedule_WithoutClientId_ReturnsBadRequest()
    {
        using var factory = new CustomWebAppFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            PeriodStart = new DateTime(2026, 11, 16),
            PeriodEnd = new DateTime(2026, 11, 17)
        };
        var response = await client.PostAsJsonAsync("/schedules/generate", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task GenerateSchedule_WithInvalidDates_ReturnsBadRequest()
    {
        using var factory = new CustomWebAppFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Client-Id", "1");
        var request = new
        {
            PeriodStart = new DateTime(2026, 11, 17),
            PeriodEnd = new DateTime(2026, 11, 16)
        };
        var response = await client.PostAsJsonAsync("/schedules/generate", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task GenerateSchedule_SavesScheduleToDatabase()
    {
        using var factory = new CustomWebAppFactory();
        await factory.InitializeDbAsync();

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Id", "1");

        var request = new
        {
            PeriodStart = new DateTime(2026, 11, 16),
            PeriodEnd = new DateTime(2026, 11, 17)
        };
        var response = await client.PostAsJsonAsync("/schedules/generate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var schedule = await db.WorkSchedules
            .IgnoreQueryFilters()
            .Include(s => s.Assignments)
            .SingleAsync();

        Assert.Equal("1", schedule.ClientId);
        Assert.Equal(request.PeriodStart, schedule.PeriodStart);
        Assert.Equal(request.PeriodEnd, schedule.PeriodEnd);

        Assert.Equal(2, schedule.Assignments.Count);
        Assert.All(schedule.Assignments, assignment => Assert.Equal("1", assignment.ClientId));
    }
    [Fact]
    public async Task DatabaseQueryFilters_IsolateClients()
    {
        using var factory = new CustomWebAppFactory();
        await factory.InitializeDbAsync();

        // Client 1
        using var scope1 = factory.Services.CreateScope();

        var context1 = scope1.ServiceProvider.GetRequiredService<ClientContext>();
        context1.ClientId = "1";

        var db1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var client1Employees = await db1.Employees.ToListAsync();
        var client1Shifts = await db1.Shifts.ToListAsync();

        // Client 2
        using var scope2 = factory.Services.CreateScope();

        var context2 = scope2.ServiceProvider.GetRequiredService<ClientContext>();
        context2.ClientId = "2";

        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var client2Employees = await db2.Employees.ToListAsync();
        var client2Shifts = await db2.Shifts.ToListAsync();

        Assert.NotEmpty(client1Employees);
        Assert.NotEmpty(client2Employees);

        Assert.All(client1Employees, e => Assert.Equal("1", e.ClientId));
        Assert.All(client2Employees, e => Assert.Equal("2", e.ClientId));
        Assert.All(client1Shifts, s => Assert.Equal("1", s.ClientId));
        Assert.All(client2Shifts, s => Assert.Equal("2", s.ClientId));
        Assert.Empty(client1Employees.Select(e => e.Id)
            .Intersect(client2Employees.Select(e => e.Id)));
        Assert.Empty(client1Shifts.Select(s => s.Id)
                .Intersect(client2Shifts.Select(s => s.Id)));
    }
}