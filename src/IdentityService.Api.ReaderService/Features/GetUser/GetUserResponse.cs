// FILE: src/IdentityService.Api.ReaderService/Features/GetUser/GetUserResponse.cs
// VERSION: 1.0.0

using IdentityService.Shared.Dtos;

namespace IdentityService.Api.ReaderService.Features.GetUser;

/// <summary>
/// Response wrapper for GetUser feature.
/// VSA feature: GetUser (ReaderService)
/// </summary>
public class GetUserResponse
{
    public UserDocumentDto User { get; set; } = null!;
}
