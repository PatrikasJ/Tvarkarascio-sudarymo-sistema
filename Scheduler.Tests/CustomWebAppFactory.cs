using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scheduler.Api.Infrastructure;
using Scheduler.Api.Security;

namespace Scheduler.Tests;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    public CustomWebAppFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Issuer", "Scheduler.Api");
        Environment.SetEnvironmentVariable("Jwt__Audience", "Scheduler.Client");
        Environment.SetEnvironmentVariable("Jwt__Key", "TEST-ONLY-JWT-SIGNING-KEY-AT-LEAST-32-BYTES-LONG");
    }
    
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"scheduler-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "Scheduler.Api",
                    ["Jwt:Audience"] = "Scheduler.Client",
                    ["Jwt:Key"] = "TEST-ONLY-JWT-SIGNING-KEY-AT-LEAST-32-BYTES-LONG"
                });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(
                options => options.UseSqlite($"Data Source ={_databasePath},Pooling=False"));
        });
    }

    public async Task InitializeDbAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await SeedData.InitializeAsync(db);
        
        var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();
        await UserSeedData.InitializeAsync(db, passwords);
    }   
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var path = _databasePath + suffix;
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}