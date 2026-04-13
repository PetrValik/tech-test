namespace OrderApi.Common.Endpoints;

/// <summary>
/// Defines the contract for a self-contained API endpoint that can register
/// its own routes onto the application's request pipeline.
/// Implement this interface in each feature folder and the route will be
/// picked up automatically at startup — no manual wiring required.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Registers the endpoint's route(s) onto the provided route builder.
    /// </summary>
    /// <param name="routeBuilder">The endpoint route builder used to map routes.</param>
    void MapEndpoint(IEndpointRouteBuilder routeBuilder);
}
