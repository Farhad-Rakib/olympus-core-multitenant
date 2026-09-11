namespace ProjectNamePlaceholder.Application.Common.Configuration;

// Which cultures the API will format and validate in. This does NOT localise API message strings
// (they are English) -- it sets CultureInfo.CurrentCulture/CurrentUICulture per request so that
// FluentValidation's built-in messages, number/date parsing and any IStringLocalizer a module adds
// all agree on one culture. Add a culture here only when there is something to show in it.
public sealed class LocalizationOptions
{
    public string DefaultCulture { get; set; } = "en-US";

    public string[] SupportedCultures { get; set; } = ["en-US"];
}
