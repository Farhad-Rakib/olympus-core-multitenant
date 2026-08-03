using FluentValidation;
using OlympusCoreMultitenant.Application.Auth.Dtos;

namespace OlympusCoreMultitenant.Application.Auth.Validation;

public sealed class RevokeRefreshTokenRequestDtoValidator : AbstractValidator<RevokeRefreshTokenRequestDto>
{
    public RevokeRefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
