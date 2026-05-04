// FILE: src/IdentityService.Api.ReaderService/Features/QueryUsers/QueryUsersResponse.cs
// VERSION: 1.0.0

using IdentityService.Shared.Dtos;

namespace IdentityService.Api.ReaderService.Features.QueryUsers;

/// <summary>
/// Response wrapper for QueryUsers feature.
/// VSA feature: QueryUsers (ReaderService)
/// </summary>
public class QueryUsersResponse
{
    public List<UserDocumentDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
}
