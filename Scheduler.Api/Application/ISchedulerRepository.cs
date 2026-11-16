using Scheduler.Api.Domain;

namespace Scheduler.Api.Application;

public interface ISchedulerRepository
{
    Task<List<Employee>> GetEmployeesAsync(string clientId);

    Task<List<Shift>> GetShiftsAsync(string clientId, DateTime periodStart, DateTime periodEnd);

    Task<WorkSchedule> SaveScheduleAsync(WorkSchedule schedule);
    
    Task<WorkSchedule?> GetScheduleByIdAsync(string clientId, int scheduleId);
    
    Task<List<WorkSchedule>> GetSchedulesAsync(string clientId);
}