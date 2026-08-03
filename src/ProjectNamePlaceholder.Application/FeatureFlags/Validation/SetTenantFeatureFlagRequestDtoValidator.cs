using FluentValidation;
using ProjectNamePlaceholder.Application.FeatureFlags.Dtos;

namespace ProjectNamePlaceholder.Application.FeatureFlags.Validation;

public sealed class SetTenantFeatureFlagRequestDtoValidator : AbstractValidator<SetTenantFeatureFlagRequestDto>
{
    public SetTenantFeatureFlagRequestDtoValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RolloutPercentage).InclusiveBetween(0, 100);
    }
}
