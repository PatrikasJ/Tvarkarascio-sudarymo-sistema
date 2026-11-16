using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Application;
using Scheduler.Api.Contracts;
using Scheduler.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ClientContext>();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=scheduler.db"));
builder.Services.AddScoped<ISchedulerRepository, EfSchedulerRepository>();
builder.Services.AddScoped<ScheduleGenerator>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await SeedData.InitializeAsync(db);
    }
}
app.Use(async (context, next) =>
    {
        var clientContext = context.RequestServices.GetRequiredService<ClientContext>();
        var clientId = context.Request.Headers["X-Client-Id"].ToString();

        if (clientId != "1" && clientId != "2")
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Choose client ID: 1 or 2"
            });
            return;
        }
        clientContext.ClientId = clientId;
        await next(context);
    }
);

app.MapGet("/", () => "Employee Scheduling API is running!");

app.MapPost("/schedules/generate", async (GenerateScheduleRequest request,
        ClientContext client, ISchedulerRepository repository, ScheduleGenerator generator) =>
    {
        if (request.PeriodEnd <= request.PeriodStart)
        {
            return Results.BadRequest(new { error = "Period end must be after start." });
        }

        var clientId = client.ClientId;
        var employees = await repository.GetEmployeesAsync(clientId);
        var shifts =
            await repository.GetShiftsAsync(clientId, request.PeriodStart, request.PeriodEnd);

        var result = generator.Generate(clientId, request.PeriodStart, request.PeriodEnd, employees, shifts);
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
);
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
});
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
});
app.Run();

public partial class Program
{
    
}