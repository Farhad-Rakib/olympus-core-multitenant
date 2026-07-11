namespace OlympusCoreMultitenant.Application.Tenants.Dtos;

// ModuleKeys are Business-module keys (see Application/Security/Modules.cs) to entitle at
// provision time. Core is entitled automatically and should not be listed here.
public sealed record ProvisionTenantRequestDto(IReadOnlyList<string>? ModuleKeys = null);
