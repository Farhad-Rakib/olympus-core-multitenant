namespace ProjectNamePlaceholder.Application.Tenants.Dtos;

public sealed record TenantDomainDto(long Id, string Domain);

public sealed record AddTenantDomainRequestDto(string Domain);
