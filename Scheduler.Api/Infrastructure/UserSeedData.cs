using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Domain;
using Scheduler.Api.Security;

namespace Scheduler.Api.Infrastructure;

public static class UserSeedData
{
    public static async Task InitializeAsync(AppDbContext db, PasswordService passwords)
    {
        if (await db.Users.AnyAsync())
            return;
        var admin1 = CreateUser("admin1@example.com", "password123", passwords);
        admin1.AddMembership("1", "Admin");
        
        var viewer1 = CreateUser("viewer1@example.com", "password123", passwords);
        viewer1.AddMembership("1", "Viewer");
        
        var admin2 = CreateUser("admin2@example.com", "password123", passwords);
        admin2.AddMembership("2", "Admin");
        
        db.Users.AddRange(admin1, viewer1, admin2);
        await db.SaveChangesAsync();
    }

    private static User CreateUser(string email, string password, PasswordService passwords)
    {
        var user = new User(email, "temp");
        user.SetPasswordHash(passwords.HashPassword(user, password));
        return user;
    }
}