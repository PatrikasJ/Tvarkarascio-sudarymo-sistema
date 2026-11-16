using Scheduler.Api.Application;
using Scheduler.Api.Domain;
using Xunit;

namespace Scheduler.Tests;

public class ScheduleGenTests
{
    private static T WithId<T>(T entity, int id) where T : class
    {
        var property = typeof(T).GetProperty("Id");
        if (property == null)
        {
            throw new InvalidOperationException("Could not find Id.");
        }
        property.SetValue(entity, id);

        return entity;
    }

    [Fact]
    public void Generate_AssignsEmployeeWithMatchingRole()
    {
        var start = new DateTime(2026, 11, 16, 8, 0, 0);
        var end = start.AddHours(4);

        var padavejas = WithId(new Employee("1", "Benas", "padavejas", 40), 1);
        var virejas = WithId(new Employee("1", "Andrius", "virejas", 40), 2);
        
        padavejas.AddAvailability(start, end);
        virejas.AddAvailability(start, end);

        var shift = WithId(new Shift("1", start, end, "virejas"), 10);
        var generator = new ScheduleGenerator();
        
        var result = generator.Generate("1", start, end,
            new List<Employee> { padavejas, virejas },
            new List<Shift> { shift });

        var assignment = Assert.Single(result.Schedule.Assignments);
        
        Assert.Equal(10, assignment.ShiftId);
        Assert.Equal(2, assignment.EmployeeId);
        Assert.Empty(result.UnfilledShiftIds);
    }

    [Fact]
    public void Generate_DoesNotAssignUnavailableEmployee()
    {
        var start = new DateTime(2026, 11, 16, 8, 0, 0);
        var end = start.AddHours(4);
        
        var padavejas = WithId(new Employee("1", "Benas", "padavejas", 40), 1);
        padavejas.AddAvailability(start.AddHours(-2), end.AddHours(-2));
        
        var shift = WithId(new Shift("1", start, end, "padavejas"), 10);
        var generator = new ScheduleGenerator();

        var result = generator.Generate("1", start, end,
            new List<Employee> { padavejas }, 
            new List<Shift> { shift });
        
        Assert.Empty(result.Schedule.Assignments);
        Assert.Contains(10, result.UnfilledShiftIds);
    }

    [Fact]
    public void Generate_DoesNotAssignOverlappingShifts()
    {
        var start = new DateTime(2026, 11, 16, 8, 0, 0);
        var end = start.AddHours(8);

        var padavejas = WithId(new Employee("1", "Benas", "padavejas", 40), 1);
        
        padavejas.AddAvailability(start, end);

        var shift1 = WithId(new Shift("1", start, start.AddHours(4), "padavejas"), 10);
        var shift2 = WithId(new Shift("1", start.AddHours(2), start.AddHours(6), "padavejas"), 11);
        var generator = new ScheduleGenerator();

        var result = generator.Generate("1", start, end,
            new List<Employee> { padavejas },
            new List<Shift> { shift1, shift2 });

        var assignment = Assert.Single(result.Schedule.Assignments);
        
        Assert.Equal(10, assignment.ShiftId);
        Assert.Contains(11, result.UnfilledShiftIds);
    }
    
    [Fact]
    public void Generate_RespectsMaxHours()
    {
        var start = new DateTime(2026, 11, 16, 8, 0, 0);
        var end = start.AddHours(10);

        var padavejas = WithId(new Employee("1", "Benas", "padavejas", 6), 1);
        
        padavejas.AddAvailability(start, end);

        var shift1 = WithId(new Shift("1", start, start.AddHours(4), "padavejas"), 10);
        var shift2 = WithId(new Shift("1", start.AddHours(4), start.AddHours(8), "padavejas"), 11);

        var generator = new ScheduleGenerator();

        var result = generator.Generate("1", start, end,
            new List<Employee> { padavejas },
            new List<Shift> { shift1, shift2 });
        
        Assert.Single(result.Schedule.Assignments);
        Assert.Contains(11, result.UnfilledShiftIds);
    }
    
    [Fact]
    public void Generate_ChoosesEmployeeWithFewerAssignedHours()
    {
        // Arrange
        var start = new DateTime(2026, 11, 16, 8, 0, 0);
        var end = start.AddHours(8);

        var employee1 = WithId(new Employee("1", "Benas", "padavejas", 40), 1);
        var employee2 = WithId(new Employee("1", "Ieva", "padavejas", 40), 2);

        employee1.AddAvailability(start, end);
        employee2.AddAvailability(start, end);

        var shift1 = WithId(new Shift("1", start, start.AddHours(4), "padavejas"), 10);
        var shift2 = WithId(new Shift("1", start.AddHours(4), end, "padavejas"), 11);

        var generator = new ScheduleGenerator();
        var result = generator.Generate("1", start, end,
            new List<Employee> { employee1, employee2 },
            new List<Shift> { shift1, shift2 });
        
        Assert.Equal(2, result.Schedule.Assignments.Count);
        Assert.Contains(result.Schedule.Assignments, a => a.ShiftId == 10 && a.EmployeeId == 1);
        Assert.Contains(result.Schedule.Assignments, a => a.ShiftId == 11 && a.EmployeeId == 2);
    }
    
    [Fact]
    public void Generate_DoesNotAssignEmployeeFromAnotherClient()
    {
        var start = new DateTime(2026, 11, 16, 8, 0, 0);
        var end = start.AddHours(4);

        var employee = WithId(new Employee("2", "Tomas", "padavejas", 40), 1);
        employee.AddAvailability(start, end);

        var shift = WithId(new Shift("1", start, end, "padavejas"), 10);

        var generator = new ScheduleGenerator();
        var result = generator.Generate("1", start, end,
            new List<Employee> { employee },
            new List<Shift> { shift });
        
        Assert.Empty(result.Schedule.Assignments);
        Assert.Contains(10, result.UnfilledShiftIds);
    }
}