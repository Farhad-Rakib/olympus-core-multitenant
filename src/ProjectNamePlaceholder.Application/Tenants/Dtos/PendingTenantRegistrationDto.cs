namespace ProjectNamePlaceholder.Application.Tenants.Dtos;

public sealed record PendingTenantRegistrationDto(string FullName, string Email, DateTime SubmittedAtUtc);
