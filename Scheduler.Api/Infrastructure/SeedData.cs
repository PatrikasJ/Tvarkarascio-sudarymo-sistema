using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Domain;

namespace Scheduler.Api.Infrastructure;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Employees.IgnoreQueryFilters().AnyAsync())
        {
            return;
        }
        var jonas = new Employee("1", "Jonas", "Padavejas", 40);
        var petras = new Employee("1", "Petras", "Padavejas", 40);
        var ona = new Employee("2", "Ona", "Padavejas", 30);
        var vytautas = new Employee("2", "Vytautas", "Virejas", 50);

        var pirmadienis = new DateTime(2026, 11, 16);
        jonas.AddAvailability(pirmadienis.AddHours(8),pirmadienis.AddHours(18));
        petras.AddAvailability(pirmadienis.AddHours(8),pirmadienis.AddHours(18));
        ona.AddAvailability(pirmadienis.AddHours(8),pirmadienis.AddHours(18));
        vytautas.AddAvailability(pirmadienis.AddHours(8),pirmadienis.AddHours(18));

        var shifts = new List<Shift>
        {
            new Shift("1", pirmadienis.AddHours(8), pirmadienis.AddHours(12), "Padavejas"),
            new Shift("1", pirmadienis.AddHours(12), pirmadienis.AddHours(16), "Padavejas"),
            new Shift("2", pirmadienis.AddHours(8), pirmadienis.AddHours(12), "Padavejas"),
            new Shift("2", pirmadienis.AddHours(8), pirmadienis.AddHours(16), "Virejas"),
        };
        
        db.Employees.AddRange(jonas, petras, ona, vytautas);
        db.Shifts.AddRange(shifts);
        
        await db.SaveChangesAsync();
    }
}