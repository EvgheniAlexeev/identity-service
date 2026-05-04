using IdentityService.Api.WriterService.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Api.WriterService;

/// <summary>
/// BLOCK_DI dependency injection registration for the Writer Service module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Writer Service handlers and controllers.
    /// </summary>
    public static IServiceCollection AddWriterApi(this IServiceCollection services)
    {
        // Handlers
        services.AddScoped<ICreateUserHandler, CreateUserHandler>();
        services.AddScoped<IAssignRoleHandler, AssignRoleHandler>();

        // Controllers are auto-registered by AddControllers()
        return services;
    }
}
