namespace Scheduler.Api.Domain;

public class Shift
{
    public int Id { get; private set; }
    public string ClientId { get; private set; } = "";
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public string RequiredRole { get; private set; } = "";
    
    private Shift() { }

    public  Shift (string clientId, DateTime start, DateTime end, string requiredRole)
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID is required.");
        if (end <= start) throw new ArgumentException("Shift end must be after start.");
        if (string.IsNullOrWhiteSpace(requiredRole)) throw new ArgumentException("Required role is missing.");
        if (end <= start) throw new ArgumentException("Availability end must be after start.");

        ClientId = clientId;
        Start = start;
        End = end;
        RequiredRole = requiredRole;
    }
}