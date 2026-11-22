using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scheduler.Api.Application;
using Scheduler.Api.Contracts;
using Scheduler.Api.Domain;
using Scheduler.Api.Infrastructure;
using Scheduler.Api.Security;


var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "Scheduler.Api",
            ["Jwt:Audience"] = "Scheduler.Client",
            ["Jwt:Key"] = "TEST-ONLY-JWT-SIGNING-KEY-AT-LEAST-32-BYTES-LONG"
        });
}

#region Services
builder.Services.AddScoped<ClientContext>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=scheduler.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<ISchedulerRepository, EfSchedulerRepository>();
builder.Services.AddScoped<ScheduleGenerator>();
builder.Services.AddSingleton<PasswordService>();
#region JWT authentication
builder.Services.AddScoped<JwtTokenService>();
var jwtKey = builder.Configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT Key is missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is missing.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is missing.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32) throw new InvalidOperationException("JWT key must contain at least 32 bytes.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            NameClaimType = "sub"
        };
    });
builder.Services.AddAuthorization();
#endregion
builder.Services.AddScoped<OrgAccessService>();
#endregion

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(db);
    if (app.Environment.IsDevelopment())
    {
        await UserSeedData.InitializeAsync(db, passwords);
    }
}

#region Middleware
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/auth"))
        {
            await next(context);
            return;
        }
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }
        
        var clientId = context.Request.Headers["X-Client-Id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new {error = "missing client ID."});
            return;
        }
        if (!int.TryParse(context.User.FindFirst("sub")?.Value, out int userId))
        {
            context.Response.StatusCode = 401;
            return;
        }
        
        var accessService = context.RequestServices.GetRequiredService<OrgAccessService>();
        var role = await accessService.GetRoleAsync(userId, clientId);
        if (role is null)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new {error = "Doesnt belong to organization."});
            return;
        }
        var clientContext = context.RequestServices.GetRequiredService<ClientContext>();
        clientContext.ClientId = clientId;
        clientContext.Role = role;
        await next(context);
    }
);
#endregion

#region Endpoints
app.MapGet("/", () => "Employee Scheduling API is running!");
app.MapPost("/schedules/generate", async (GenerateScheduleRequest request, AppDbContext db,
        ClientContext client, ISchedulerRepository repository, ScheduleGenerator generator) =>
    {
        if (client.Role != "Admin")
        {
            return Results.Forbid();
        }
        if (request.PeriodEnd <= request.PeriodStart)
        {
            return Results.BadRequest(new { error = "Period end must be after start." });
        }

        var clientId = client.ClientId;
        var employees = await repository.GetEmployeesAsync(clientId);
        var shifts =
            await repository.GetShiftsAsync(clientId, request.PeriodStart, request.PeriodEnd);
        var settings = await db.SchedulingSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();
        int maxShiftsPerEmployee = settings?.MaxShiftsPerEmployee ?? 3;

        var result = generator.Generate(clientId, request.PeriodStart, request.PeriodEnd, employees, shifts,
            maxShiftsPerEmployee);
        var savedSchedule = await repository.SaveScheduleAsync(result.Schedule);

        return Results.Ok(new
        {
            ScheduleId = savedSchedule.Id,
            ClientId = savedSchedule.ClientId,
            PeriodStart = savedSchedule.PeriodStart,
            PeriodEnd = savedSchedule.PeriodEnd,
            Asssignments = savedSchedule.Assignments.Select(a => new { a.ShiftId, a.EmployeeId, }),
            result.UnfilledShiftIds
        });
    }
).RequireAuthorization();
app.MapGet("/schedules/{id:int}", async (int id, ClientContext client, ISchedulerRepository repository) =>
{
    var schedule = await repository.GetScheduleByIdAsync(client.ClientId, id);
    if (schedule is null)
    {
        return Results.NotFound(new
        {
            error = "Schedule not found."
        });
    }

    var response = new ScheduleResponse(
        schedule.Id,
        schedule.ClientId,
        schedule.PeriodStart,
        schedule.PeriodEnd,
        schedule.CreatedAt,
        schedule.Assignments
            .Select(a => new ScheduleAssignmentResponse(
                a.ShiftId,
                a.EmployeeId))
            .ToList());

    return Results.Ok(response);
}).RequireAuthorization();
app.MapGet("/schedules", async (ClientContext client, ISchedulerRepository repository) =>
{
    var schedules = await repository.GetSchedulesAsync(client.ClientId);
    var response = schedules.Select(schedule =>
        new ScheduleResponse(
            schedule.Id,
            schedule.ClientId,
            schedule.PeriodStart,
            schedule.PeriodEnd,
            schedule.CreatedAt,
            schedule.Assignments
                .Select(a => new ScheduleAssignmentResponse(
                    a.ShiftId,
                    a.EmployeeId))
                .ToList()
        )).ToList();

    return Results.Ok(response);
}).RequireAuthorization();
app.MapPost("/auth/login",
    async (LoginRequest request, AppDbContext db, PasswordService passwords, JwtTokenService tokens) =>
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email or password is missing." });
        }
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .AsNoTracking().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !passwords.VerifyPassword(user, request.Password))
        {
            return Results.Unauthorized();
        }
        
        var (token, expiresAt) = tokens.CreateToken(user);
        return Results.Ok(new LoginResponse(token, "Bearer", expiresAt));
    });
app.MapGet("/auth/me", (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        UserId = user.FindFirstValue("sub"),
        Email = user.FindFirstValue("email")
    });
}).RequireAuthorization();
app.MapGet("/settings/scheduling", async (AppDbContext db, ClientContext clientContext) =>
{
    var settings = await db.SchedulingSettings
        .AsNoTracking()
        .FirstOrDefaultAsync();
    const int defaultMaxShifts = 3;

    return Results.Ok(new SchedulingSettingsResponse(
        clientContext.ClientId,
        settings?.MaxShiftsPerEmployee ?? defaultMaxShifts));
}).RequireAuthorization();
app.MapPut("/settings/scheduling",
    async (UpdateSchedulingSettingsRequest request, AppDbContext db, ClientContext clientContext) =>
    {
        if (clientContext.Role != "Admin")
            return Results.Forbid();
        if (request.MaxShiftsPerEmployee < 1)
            return Results.BadRequest(new { error = "MaxShiftsPerEmployee must be greater than 0." });

        var settings = await db.SchedulingSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new SchedulingSettings(clientContext.ClientId, request.MaxShiftsPerEmployee);
            db.SchedulingSettings.Add(settings);
        }
        else
        {
            settings.UpdateMaxShifts(request.MaxShiftsPerEmployee);
        }

        await db.SaveChangesAsync();
        return Results.Ok(new SchedulingSettingsResponse(settings.ClientId, settings.MaxShiftsPerEmployee));
    }).RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
#endregion

app.Run();

public partial class Program { }