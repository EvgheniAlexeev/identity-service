// FILE: DependencyInjection.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: M-IDENTITY-READER component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_READER

// FILE: src/IdentityService.Api.ReaderService/Features/DependencyInjection.cs
// VERSION: 1.0.0

using IdentityService.Api.ReaderService.Features.GetUser;
using IdentityService.Api.ReaderService.Features.QueryUsers;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Api.ReaderService.Features;

/// <summary>
/// Registers VSA feature handlers and validators for the ReaderService.
/// </summary>
public static class VsaFeatureRegistration
{
    public static IServiceCollection AddReaderServiceFeatures(this IServiceCollection services)
    {
        // GetUser feature
        services.AddScoped<GetUserHandler>();
        services.AddScoped<GetUserEndpoint>();
        services.AddScoped<GetUserValidator>();
        services.AddValidatorsFromAssemblyContaining<GetUserValidator>();

        // QueryUsers feature
        services.AddScoped<QueryUsersHandler>();
        services.AddScoped<QueryUsersEndpoint>();
        services.AddValidatorsFromAssemblyContaining<QueryUsersValidator>();

        return services;
    }
}
