using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using ProjectNamePlaceholder.Application.Common.Interfaces.Services;
using ProjectNamePlaceholder.Infrastructure.Authentication;
using ProjectNamePlaceholder.Infrastructure.Email;
using ProjectNamePlaceholder.Infrastructure.MultiTenancy;
using ProjectNamePlaceholder.Infrastructure.Security;

namespace ProjectNamePlaceholder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITenantSmtpSettingsProvider, TenantSmtpSettingsProvider>();
        services.AddSingleton<ICurrentTenantService, CurrentTenantService>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();

        // Same shape as Caching:UseRedis / Integration:UseRabbitMq -- both implementations always
        // ship and config picks one, so swapping providers never needs a code change.
        var provider = configuration.GetSection(EmailOptions.SectionName)[nameof(EmailOptions.Provider)];
        if (string.Equals(provider, EmailOptions.LogProvider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailService, LoggingEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }

        return services;
    }
}
