using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Application;
using Scheduler.Api.Domain;

namespace Scheduler.Api.Infrastructure;

public class EfSchedulerRepository: ISchedulerRepository
{
    private readonly AppDbContext _db;
    private readonly ClientContext _client;
    
    public EfSchedulerRepository(AppDbContext db, ClientContext client)
    {
        _db = db;
        _client = client;
    }
    
    public async Task<List<Employee>> GetEmployeesAsync(string clientId)
    {
        EnsureClient(clientId);
        
        return await _db.Employees
            .Include(e => e.Availability)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Shift>> GetShiftsAsync(string clientId, DateTime periodStart, DateTime periodEnd)
    {
        EnsureClient(clientId);

        if (periodStart >= periodEnd)
        {
            throw new ArgumentException("The period start must be before period end.");
        }

        return await _db.Shifts
            .Where(shift => shift.Start >= periodStart && shift.Start <= periodEnd)
            .OrderBy(shift => shift.Start)
            .ThenBy(shift => shift.Id)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<WorkSchedule> SaveScheduleAsync(WorkSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        EnsureClient(schedule.ClientId);

        var assignments = schedule.Assignments;
        if (assignments.Any(a => a.ClientId != schedule.ClientId))
        {
            throw new ArgumentException("Assignment client does not match scheduler client.");
        }

        if (assignments.Select(a => a.ShiftId).Distinct().Count() != assignments.Count)
        {
            throw new ArgumentException("Shift cannot have multiple assignments in one schedule.");
        }
        var employeeIds = assignments.Select(a => a.EmployeeId).Distinct().ToList();
        var shiftIds = assignments.Select(a => a.ShiftId).Distinct().ToList();

        var validEmployeeCount = await _db.Employees.CountAsync(e => employeeIds.Contains(e.Id));
        var validShiftCount = await _db.Shifts.CountAsync(e => shiftIds.Contains(e.Id));
        if (validEmployeeCount != employeeIds.Count || validShiftCount != shiftIds.Count)
        {
            throw new ArgumentException("Schedule references outside of current client.");
        }
        
        _db.WorkSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        
        return schedule;
    }

    public async Task<WorkSchedule?> GetScheduleByIdAsync(string clientId, int scheduleId)
    {
        EnsureClient(clientId);
        
        return await _db.WorkSchedules
            .Include(s=>s.Assignments)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
    }

    public async Task<List<WorkSchedule>> GetSchedulesAsync(string clientId)
    {
        EnsureClient(clientId);

        return await _db.WorkSchedules
            .Include(s => s.Assignments)
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .AsNoTracking()
            .ToListAsync();
    }
    
    private void EnsureClient(string scheduleClientId)
    {
        if (string.IsNullOrWhiteSpace(_client.ClientId))
        {
            throw new InvalidOperationException("No client selected.");
        }

        if (_client.ClientId != scheduleClientId)
        {
            throw new ArgumentException("Client selected does not match current client.");
        }
    }
}