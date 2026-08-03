using FluentValidation;
using ProjectNamePlaceholder.Application.Tenants.Dtos;

namespace ProjectNamePlaceholder.Application.Tenants.Validation;

public sealed class SubmitTenantRegistrationRequestDtoValidator : AbstractValidator<SubmitTenantRegistrationRequestDto>
{
    public SubmitTenantRegistrationRequestDtoValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
