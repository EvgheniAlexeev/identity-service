using IdentityService.Api.ReaderService.Handlers;
using IdentityService.Api.ReaderService.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Api.ReaderService;

/// <summary>
/// BLOCK_DI dependency injection registration for the Reader Service module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Reader Service handlers, validators, and controllers.
    /// </summary>
    public static IServiceCollection AddReaderApi(this IServiceCollection services)
    {
        // Handlers
        services.AddScoped<IGetUserHandler, GetUserHandler>();
        services.AddScoped<IGetRoleHandler, GetRoleHandler>();

        // Validators
        services.AddScoped<GetUserRequestValidator>();

        // Controllers are auto-registered by AddControllers()
        return services;
    }
}
