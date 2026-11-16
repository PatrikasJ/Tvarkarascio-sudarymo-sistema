using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Domain;

namespace Scheduler.Api.Infrastructure;

public sealed class AppDbContext : DbContext
{
    private readonly ClientContext _client;
    public string CurrentClientId => _client.ClientId;
    
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    
    public AppDbContext(DbContextOptions<AppDbContext> options, ClientContext client) : base(options)
    {
        _client = client;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Employee>()
            .HasMany(e=>e.Availability)
            .WithOne()
            .HasForeignKey(a=>a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity <WorkSchedule>()
            .HasMany(s => s.Assignments)
            .WithOne()
            .HasForeignKey(a=>a.WorkScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ShiftAssignment>()
            .HasOne<Shift>()
            .WithMany()
            .HasForeignKey(a=>a.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ShiftAssignment>()
            .HasOne<Employee>()
            .WithMany()
            .HasForeignKey(a=>a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<ShiftAssignment>()
            .HasIndex(a => new { a.WorkScheduleId, a.ShiftId })
            .IsUnique();
        
        modelBuilder.Entity<Employee>()
            .HasQueryFilter(e => e.ClientId == CurrentClientId);
        modelBuilder.Entity<Availability>()
            .HasQueryFilter(e => e.ClientId == CurrentClientId);
        modelBuilder.Entity<Shift>()
            .HasQueryFilter(e => e.ClientId == CurrentClientId);
        modelBuilder.Entity<WorkSchedule>()
            .HasQueryFilter(e => e.ClientId == CurrentClientId);
        modelBuilder.Entity<ShiftAssignment>()
            .HasQueryFilter(e => e.ClientId == CurrentClientId);
        
    }
}