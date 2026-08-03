using FluentValidation;

namespace OlympusCoreMultitenant.BusinessModules.SamplePlaceholderModule.Features.SampleFeature;

public sealed class SampleFeatureRequestValidator : AbstractValidator<SampleFeatureRequest>
{
    public SampleFeatureRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
