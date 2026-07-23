namespace OlympusCoreMultitenant.Application.Common.Dtos;

public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
