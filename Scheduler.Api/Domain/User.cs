namespace Scheduler.Api.Domain;

public sealed class User
{
    public int Id { get; private set; }
    public string Email { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public List<OrgMembership> Memberships { get; private set; } = new();
    
    private User() { }

    public User(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required.");
        
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }

    public void AddMembership(string clientId, string role)
    {
        if(Memberships.Any(m => m.ClientId == clientId))
            throw new ArgumentException("User already belongs to this organization.");
        Memberships.Add(new OrgMembership(clientId, role));
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required.");
        PasswordHash = passwordHash;
    }
}