using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Infrastructure;

namespace Scheduler.Api.Security;

public sealed class OrgAccessService
{
    private readonly AppDbContext _db;

    public OrgAccessService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetRoleAsync(int userId, string clientId)
    {
        return await _db.OrgMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.ClientId == clientId)
            .Select(m => m.Role)
            .FirstOrDefaultAsync();
    }
}