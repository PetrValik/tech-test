namespace OrderApi.Common.Endpoints;

/// <summary>
/// Extension methods for registering and mapping <see cref="IEndpoint"/> implementations.
/// Two steps are required:
/// 1. Call <see cref="AddEndpoints"/> in the service registration phase to scan the assembly
///    and register every concrete <see cref="IEndpoint"/> as a transient service.
/// 2. Call <see cref="MapEndpoints"/> in the pipeline configuration phase to resolve those
///    services and invoke each endpoint's <see cref="IEndpoint.MapEndpoint"/> method.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Scans the entry assembly for all non-abstract, non-interface types that implement
    /// <see cref="IEndpoint"/> and registers each one as a transient
    /// <see cref="IEndpoint"/> service in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add the endpoints to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpointTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(endpointType => endpointType is { IsAbstract: false, IsInterface: false }
                        && endpointType.IsAssignableTo(typeof(IEndpoint)));

        foreach (var endpointType in endpointTypes)
        {
            services.AddTransient(typeof(IEndpoint), endpointType);
        }

        return services;
    }

    /// <summary>
    /// Resolves all registered <see cref="IEndpoint"/> services from the dependency
    /// injection container and calls <see cref="IEndpoint.MapEndpoint"/> on each one
    /// to register their routes onto the application's request pipeline.
    /// </summary>
    /// <param name="routeBuilder">The endpoint route builder to map routes onto.</param>
    /// <returns>The same <see cref="IEndpointRouteBuilder"/> instance for method chaining.</returns>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        var endpoints = routeBuilder.ServiceProvider
            .GetRequiredService<IEnumerable<IEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(routeBuilder);
        }

        return routeBuilder;
    }
}
