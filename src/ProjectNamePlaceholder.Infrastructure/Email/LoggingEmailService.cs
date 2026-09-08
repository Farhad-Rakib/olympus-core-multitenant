using Microsoft.Extensions.Logging;
using ProjectNamePlaceholder.Application.Common.Interfaces.Services;

namespace ProjectNamePlaceholder.Infrastructure.Email;

// Selected via Email:Provider=log. Exists so a developer can exercise password reset and the other
// mail-sending flows without a reachable SMTP server -- the default appsettings host
// (smtp.mailserver.local) does not resolve, so the smtp provider fails those flows locally.
// Never select this in an environment where mail is actually expected to arrive.
public sealed class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Email:log] Not sending mail. To={Recipient} Subject={Subject} Body={HtmlBody}",
            to, subject, htmlBody);

        return Task.CompletedTask;
    }
}
