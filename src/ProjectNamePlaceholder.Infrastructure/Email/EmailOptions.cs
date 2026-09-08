namespace ProjectNamePlaceholder.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public const string SmtpProvider = "smtp";
    public const string LogProvider = "log";

    public string Provider { get; set; } = SmtpProvider;
}
