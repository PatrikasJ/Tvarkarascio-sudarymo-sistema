namespace Scheduler.Api.Domain;

public class WorkSchedule
{
    public int Id { get; private set; }
    public string ClientId { get; private set; } = "";
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public List<ShiftAssignment> Assignments { get; private set; } = [];
    
    private WorkSchedule() { }

    public WorkSchedule(string clientId, DateTime start, DateTime end)
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID is required.");
        if (end <= start) throw new ArgumentException("Availability end must be after start.");

        ClientId = clientId;
        PeriodStart = start;
        PeriodEnd = end;
        CreatedAt = DateTime.UtcNow;
    }


    public void AddAssignment(int shiftId, int employeeId)
    {
        if (Assignments.Any(a => a.ShiftId == shiftId))
        {
            throw new InvalidOperationException("Shift has employee already.");
        }
        Assignments.Add(new ShiftAssignment(ClientId, shiftId, employeeId));
    }
}