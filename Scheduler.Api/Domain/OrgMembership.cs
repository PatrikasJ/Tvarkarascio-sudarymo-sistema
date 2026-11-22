namespace Scheduler.Api.Domain;

public sealed class OrgMembership
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string ClientId { get; private set; } = "";
    public string Role { get; private set; } = "";
    
    private OrgMembership() { }

    public OrgMembership(string clientId, string role)
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID is required.");
        if (role != "Admin" && role != "Viewer") throw new ArgumentException("Invalid role.");
        
        ClientId = clientId;
        Role = role;
    }
    
}