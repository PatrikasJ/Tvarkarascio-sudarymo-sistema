namespace Scheduler.Api.Domain;

public class Availability
{
    public int Id { get; private set; }
    public int EmployeeId { get; private set; }
    public string ClientId { get; private set; } = "";
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    
    private Availability() { }

    public Availability(string clientId, DateTime start, DateTime end)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Required client id");
        if (end <= start)
            throw new ArgumentException(
                "Availability end must be after start.");
        
        ClientId = clientId;
        Start = start;
        End = end;
    }
}