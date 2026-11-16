namespace Scheduler.Api.Domain;

public class ShiftAssignment
{
    public int Id { get; private set; }
    public string ClientId { get; private set; } = "";
    public int WorkScheduleId { get; private set; }
    public int ShiftId { get; private set; }
    public int EmployeeId { get; private set; }
    
    private ShiftAssignment() { }

    public ShiftAssignment(string clientId, int shiftId, int employeeId)
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID is required.");
        if (shiftId <= 0) throw new ArgumentOutOfRangeException(nameof(shiftId));
        if (employeeId <= 0) throw new ArgumentOutOfRangeException(nameof(employeeId));
        
        ClientId = clientId;
        ShiftId = shiftId;
        EmployeeId = employeeId;
    }
}