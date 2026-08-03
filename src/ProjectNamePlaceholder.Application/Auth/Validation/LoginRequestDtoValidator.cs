using FluentValidation;
using ProjectNamePlaceholder.Application.Auth.Dtos;

namespace ProjectNamePlaceholder.Application.Auth.Validation;

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);

        // Whether TenantSlug is required depends on the admin-editable EmailUniquenessScope
        // SystemSetting, which needs an async DB read -- enforced in AuthService.LoginAsync instead
        // of here, since FluentValidation rule construction can't await a DB call.
    }
}
