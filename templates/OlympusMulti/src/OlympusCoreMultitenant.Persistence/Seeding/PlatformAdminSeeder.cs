using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OlympusCoreMultitenant.Application.Common.Interfaces.Security;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Persistence.Seeding;

// Seeds exactly ONE platform super admin, globally, ever -- not once per tenant. Must run inside
// an active ICurrentTenantService.BeginScope (the account still needs a TenantId FK), but the
// "does one already exist" check deliberately ignores query filters and tenant scope: it's a
// cross-tenant question, so an admin already seeded under a different tenant still counts.
//
// Previously (as DefaultUserSeeder) this ran unconditionally from every tenant's provisioning,
// so every tenant ended up with its own copy of the same hardcoded superadmin@localhost /
// SuperAdmin@123! credential, each independently flagged IsPlatformSuperAdmin -- a backdoor that
// multiplied with tenant count. Now it only ever creates one account, and only if none exists yet.
public static class PlatformAdminSeeder
{
    private const string DefaultEmail = "superadmin@localhost";

    public static async Task SeedAsync(DbContext dbContext, IPasswordHasher passwordHasher, IConfiguration configuration, ILogger logger)
    {
        var alreadyExists = await dbContext.Set<User>()
            .IgnoreQueryFilters()
            .AnyAsync(u => u.IsPlatformSuperAdmin && u.IsActive);
        if (alreadyExists)
        {
            return;
        }

        var email = configuration["Bootstrap:PlatformAdminEmail"];
        email = string.IsNullOrWhiteSpace(email) ? DefaultEmail : email;

        var configuredPassword = configuration["Bootstrap:PlatformAdminPassword"];
        var usingGeneratedPassword = string.IsNullOrWhiteSpace(configuredPassword);
        var password = usingGeneratedPassword ? GenerateRandomPassword() : configuredPassword!;

        var admin = new User("Platform Admin", email, passwordHasher.Hash(password));
        admin.GrantPlatformSuperAdmin();

        dbContext.Add(admin);
        await dbContext.SaveChangesAsync();

        if (usingGeneratedPassword)
        {
            logger.LogWarning(
                "Platform admin bootstrap account created: email={Email} password={Password} -- store this now, it will not be logged again. Set Bootstrap:PlatformAdminPassword to control it explicitly next time.",
                email, password);
        }
        else
        {
            logger.LogInformation("Platform admin bootstrap account created from configuration: email={Email}", email);
        }
    }

    private static string GenerateRandomPassword()
    {
        // 18 random bytes -> base64 gives ~24 chars of high entropy. Appending a fixed
        // "Aa1!" suffix guarantees upper/lower/digit/symbol variety without weakening the
        // random portion, in case a password-complexity rule is ever added later.
        var randomPart = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        return randomPart + "Aa1!";
    }
}
