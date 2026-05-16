// FILE: DependencyInjection.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY
// PURPOSE: M-IDENTITY component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY

using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace IdentityService.CacheLayer;

/// <summary>
/// BLOCK_DI dependency injection registration for the CacheLayer module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers cache repository services and MongoDB context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">MongoDB connection string.</param>
    /// <param name="databaseName">Database name (default: "identity_cache").</param>
    public static IServiceCollection AddCacheLayer(
        this IServiceCollection services,
        string connectionString,
        string databaseName = "identity_cache")
    {
        // Register MongoDB client (singleton per connection string)
        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));

        // Register database
        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        // Register cache context
        services.AddSingleton<CacheContext>();

        // Register repositories
        services.AddScoped<IUserCacheRepository, UserCacheRepository>();
        services.AddScoped<IRoleCacheRepository, RoleCacheRepository>();

        return services;
    }
}
