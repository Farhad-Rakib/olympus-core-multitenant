using System.Security.Cryptography;
using System.Text;
using OlympusCoreMultitenant.Application.Common.Interfaces.Security;
using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Persistence.Seeding;

public static class DefaultUserSeeder
{
    public static async Task SeedAsync(DbContext dbContext, IPasswordHasher passwordHasher)
    {
        const string superAdminEmail = "superadmin@localhost";
        const string superAdminPassword = "SuperAdmin@123!";

        if (await dbContext.Set<User>().AnyAsync(u => u.Email == superAdminEmail))
        {
            return;
        }

        var superAdmin = new User(
            fullName: "Super Admin",
            email: superAdminEmail,
            passwordHash: passwordHasher.Hash(superAdminPassword)
        );

        superAdmin.GrantPlatformSuperAdmin();

        dbContext.Add(superAdmin);
        await dbContext.SaveChangesAsync();
    }
}
