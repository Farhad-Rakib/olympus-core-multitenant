using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces.Services;
using ProjectNamePlaceholder.Infrastructure;
using ProjectNamePlaceholder.Infrastructure.Email;
using ProjectNamePlaceholder.Infrastructure.MultiTenancy;

namespace ProjectNamePlaceholder.UnitTests.Infrastructure;

public class EmailProviderSelectionTests
{
    private static Type ResolveRegisteredEmailService(string? provider)
    {
        var settings = new Dictionary<string, string?>();
        if (provider is not null)
        {
            settings[$"{EmailOptions.SectionName}:{nameof(EmailOptions.Provider)}"] = provider;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var descriptor = new ServiceCollection()
            .AddInfrastructure(configuration)
            .Single(service => service.ServiceType == typeof(IEmailService));

        return descriptor.ImplementationType!;
    }

    [Fact]
    public void Smtp_provider_is_the_default_when_no_provider_is_configured()
    {
        Assert.Equal(typeof(SmtpEmailService), ResolveRegisteredEmailService(null));
    }

    [Fact]
    public void Log_provider_is_selected_by_configuration()
    {
        Assert.Equal(typeof(LoggingEmailService), ResolveRegisteredEmailService("log"));
    }

    [Fact]
    public void Provider_selection_is_case_insensitive()
    {
        Assert.Equal(typeof(LoggingEmailService), ResolveRegisteredEmailService("LOG"));
    }

    [Fact]
    public void An_unrecognised_provider_falls_back_to_smtp_rather_than_failing_startup()
    {
        Assert.Equal(typeof(SmtpEmailService), ResolveRegisteredEmailService("sendgrid"));
    }

    [Fact]
    public async Task Log_provider_does_not_throw_and_sends_nothing()
    {
        IEmailService service = new LoggingEmailService(NullLogger<LoggingEmailService>.Instance);

        await service.SendAsync("user@acme.com", "Password Reset", "<p>hello</p>");
    }

    [Fact]
    public async Task Smtp_send_failures_surface_as_EmailDeliveryException()
    {
        // An empty From address makes MailAddress throw before any network call, which is enough to
        // prove the provider-failure path is wrapped rather than escaping as a raw exception.
        var settings = new ResolvedSmtpSettings(
            Host: "smtp.invalid",
            Port: 587,
            Username: "user",
            Password: "password",
            FromAddress: string.Empty,
            FromName: "Platform",
            UseSsl: true);

        var service = new SmtpEmailService(
            new StubSmtpSettingsProvider(settings),
            new CurrentTenantService(),
            NullLogger<SmtpEmailService>.Instance);

        var exception = await Assert.ThrowsAsync<EmailDeliveryException>(
            () => service.SendAsync("user@acme.com", "Password Reset", "<p>hello</p>"));

        Assert.Equal(502, exception.StatusCode);
        Assert.NotNull(exception.InnerException);
        Assert.Equal("user@acme.com", exception.Recipient);
    }

    [Fact]
    public void Delivery_failure_message_does_not_leak_the_recipient_to_the_client()
    {
        // GlobalExceptionHandler returns Message verbatim in the response body, so the address
        // must stay on the Recipient property (for logs) and out of the message.
        var exception = new EmailDeliveryException("user@acme.com", new InvalidOperationException("smtp down"));

        Assert.DoesNotContain("user@acme.com", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubSmtpSettingsProvider : ITenantSmtpSettingsProvider
    {
        private readonly ResolvedSmtpSettings _settings;

        public StubSmtpSettingsProvider(ResolvedSmtpSettings settings) => _settings = settings;

        public Task<ResolvedSmtpSettings> ResolveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_settings);
    }
}
