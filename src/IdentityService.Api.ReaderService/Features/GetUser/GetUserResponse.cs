// FILE: GetUserResponse.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: M-IDENTITY-READER component
// SEMANTIC_TAG: [DTO, DATA_TRANSFER]
// START_MODULE M_IDENTITY_READER

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
