using FluentValidation;
using OlympusCoreMultitenant.Application.FeatureFlags.Dtos;

namespace OlympusCoreMultitenant.Application.FeatureFlags.Validation;

public sealed class SetTenantFeatureFlagRequestDtoValidator : AbstractValidator<SetTenantFeatureFlagRequestDto>
{
    public SetTenantFeatureFlagRequestDtoValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RolloutPercentage).InclusiveBetween(0, 100);
    }
}
