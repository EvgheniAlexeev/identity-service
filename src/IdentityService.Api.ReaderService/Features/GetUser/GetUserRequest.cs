// FILE: GetUserRequest.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: M-IDENTITY-READER component
// SEMANTIC_TAG: [DTO, DATA_TRANSFER]
// START_MODULE M_IDENTITY_READER

// FILE: src/IdentityService.Api.ReaderService/Features/GetUser/GetUserRequest.cs
// VERSION: 1.0.0

using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.ReaderService.Features.GetUser;

/// <summary>
/// Request model for GetUser feature.
/// VSA feature: GetUser (ReaderService)
/// </summary>
public class GetUserRequest
{
    /// <summary>
    /// User ID to fetch from cache.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string UserId { get; set; } = string.Empty;
}
