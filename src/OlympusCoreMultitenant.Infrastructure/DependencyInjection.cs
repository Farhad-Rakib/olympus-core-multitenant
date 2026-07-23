using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.Common.Interfaces.Security;
using OlympusCoreMultitenant.Application.Common.Interfaces.Services;
using OlympusCoreMultitenant.Infrastructure.Authentication;
using OlympusCoreMultitenant.Infrastructure.Email;
using OlympusCoreMultitenant.Infrastructure.MultiTenancy;
using OlympusCoreMultitenant.Infrastructure.Security;

namespace OlympusCoreMultitenant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddSingleton<ICurrentTenantService, CurrentTenantService>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
