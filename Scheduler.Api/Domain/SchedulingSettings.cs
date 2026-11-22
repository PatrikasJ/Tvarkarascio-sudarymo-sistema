namespace Scheduler.Api.Domain;

public sealed class SchedulingSettings
{
    public int Id { get; private set; }
    public string ClientId { get; private set; } = "";
    public int MaxShiftsPerEmployee { get; private set; }
    
    private SchedulingSettings() { }

    public SchedulingSettings(string clientId, int maxShiftsPerEmployee)
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID is required.");
        if (maxShiftsPerEmployee < 1) throw new ArgumentException("Maximum shifts must be at least 1.");
        
        ClientId = clientId;
        MaxShiftsPerEmployee = maxShiftsPerEmployee;
    }

    public void UpdateMaxShifts(int maxShifts)
    {
        if (maxShifts < 1) throw new ArgumentException("Maximum shifts must be at least 1.");
        MaxShiftsPerEmployee = maxShifts;
    }
}