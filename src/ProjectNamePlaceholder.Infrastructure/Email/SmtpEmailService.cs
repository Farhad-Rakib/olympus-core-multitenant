using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Common.Interfaces.Services;

namespace ProjectNamePlaceholder.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly ITenantSmtpSettingsProvider _settingsProvider;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        ITenantSmtpSettingsProvider settingsProvider,
        ICurrentTenantService currentTenantService,
        ILogger<SmtpEmailService> logger)
    {
        _settingsProvider = settingsProvider;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsProvider.ResolveAsync(cancellationToken);
        var tenantId = _currentTenantService.TenantId;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromAddress, settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(to));

            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.UseSsl,
                Credentials = new NetworkCredential(settings.Username, settings.Password)
            };

            using var registration = cancellationToken.Register(client.SendAsyncCancel);
            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation(
                "Sent email to {Recipient} with subject {Subject} via {SmtpHost} for tenant {TenantId}.",
                to, subject, settings.Host, tenantId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Wrapped so callers surface a 502 (upstream provider failed) instead of a raw
            // SmtpException bubbling out as an opaque 500.
            _logger.LogError(
                exception,
                "Failed to send email to {Recipient} with subject {Subject} via {SmtpHost} for tenant {TenantId}.",
                to, subject, settings.Host, tenantId);

            throw new EmailDeliveryException(to, exception);
        }
    }
}
