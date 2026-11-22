using Scheduler.Api.Domain;

namespace Scheduler.Api.Application;

public sealed class ScheduleGenerator
{
    public ScheduleGenerationResult Generate(
        string clientId, DateTime periodStart, DateTime periodEnd, List<Employee> employees, List<Shift> shifts,
        int maxShiftsPerEmployee = 3)
    {
        if (maxShiftsPerEmployee < 1) throw new ArgumentException("Maximum shifts per employee must be at least 1.");
        
        var schedule = new WorkSchedule(clientId, periodStart, periodEnd);
        var unfinishedShifts = new List<int>();

        var assignedHours = employees.ToDictionary(employee => employee.Id, employee => 0.0);
        var assignedShifts = employees.ToDictionary(employee => employee.Id, employee => new List<Shift>());

        foreach (var shift in shifts.OrderBy(shift => shift.Start))
        {
            var eligibleEmployees = employees
                .Where(employee => employee.ClientId == clientId)
                .Where(employee => string.Equals(employee.Role, shift.RequiredRole, StringComparison.OrdinalIgnoreCase))
                .Where(employee => employee.Availability.Any(availability =>
                    availability.Start <= shift.Start && availability.End >= shift.End))
                .Where(employee => !assignedShifts[employee.Id].Any(existingShift =>
                    existingShift.Start < shift.End && existingShift.End > shift.Start))
                .Where(employee =>
                    assignedHours[employee.Id] + (shift.End - shift.Start).TotalHours <= employee.MaxHours)
                .Where(e => assignedShifts.Count(a => a.Key == e.Id) < maxShiftsPerEmployee);
            
            var selectedEmployee = eligibleEmployees
                .OrderBy(employee => assignedHours[employee.Id])
                .ThenBy(employee => employee.Id)
                .FirstOrDefault();

            if (selectedEmployee is null)
            {
                unfinishedShifts.Add(shift.Id);
                continue;
            }

            schedule.AddAssignment(shift.Id, selectedEmployee.Id);
            assignedHours[selectedEmployee.Id] += (shift.End - shift.Start).TotalHours;
            assignedShifts[selectedEmployee.Id].Add(shift);
        }

        return new ScheduleGenerationResult(schedule, unfinishedShifts);
    }
}
