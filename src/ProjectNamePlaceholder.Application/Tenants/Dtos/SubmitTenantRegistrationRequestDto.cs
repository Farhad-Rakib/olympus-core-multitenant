namespace ProjectNamePlaceholder.Application.Tenants.Dtos;

public sealed record SubmitTenantRegistrationRequestDto(string Token, string FullName, string Email, string Password);
