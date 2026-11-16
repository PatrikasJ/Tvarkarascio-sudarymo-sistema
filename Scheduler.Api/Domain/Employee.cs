namespace Scheduler.Api.Domain;

public class Employee
{
    public int Id { get; private set; }
    public string ClientId { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Role { get; private set; } = "";
    public int MaxHours { get; private set; }
    public List<Availability> Availability { get; private set; } = [];
    
    private Employee() { }

    public Employee(string clientId, string name, string role, int maxHours)
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Employee name is required.");
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Employee role is required.");
        if(maxHours <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHours),"MaxHours must be greater than 0");
        
        ClientId = clientId;
        Name = name;
        Role = role;
        MaxHours = maxHours;
    }

    public void ChangeHours(int hours)
    {
        if(hours <= 0)
            throw new ArgumentOutOfRangeException(nameof(hours), "Hours must be greater than 0");
        MaxHours = hours;
    }

    public void AddAvailability(DateTime start, DateTime end)
    {
        Availability.Add(new Availability(ClientId, start, end));
    }
}