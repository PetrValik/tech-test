using Microsoft.EntityFrameworkCore;
using OrderApi.Common.Endpoints;
using OrderApi.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

namespace OrderApi.Extensions;

/// <summary>
/// Configures the HTTP request pipeline — middleware order matters.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies EF Core migrations on startup (for relational databases) and
    /// seeds required reference data.
    /// </summary>
    /// <param name="app">The configured web application.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous database initialization.</returns>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var shouldApplyMigrations = app.Configuration.GetValue(
            "Database:ApplyMigrationsOnStartup",
            app.Environment.IsDevelopment());

        if (!shouldApplyMigrations)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var orderContext = scope.ServiceProvider.GetRequiredService<OrderContext>();

        // Integration tests use SQLite with EnsureCreated/EnsureDeleted in the factory.
        if (orderContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        await orderContext.Database.MigrateAsync();

        var existingStatusNames = await orderContext.OrderStatuses
            .Select(status => status.Name)
            .ToListAsync();

        var missingStatusNames = OrderStatusNames.All
            .Where(name => !existingStatusNames.Contains(name, StringComparer.Ordinal))
            .ToList();

        if (missingStatusNames.Count == 0)
        {
            return;
        }

        orderContext.OrderStatuses.AddRange(missingStatusNames.Select(name => new Infrastructure.Entities.OrderStatus
        {
            Id = Guid.NewGuid().ToByteArray(),
            Name = name
        }));

        await orderContext.SaveChangesAsync();
    }

    /// <summary>
    /// Registers all middleware in the correct order.
    /// </summary>
    /// <param name="app">The configured web application to add middleware to.</param>
    public static void UseApiMiddleware(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseResponseCompression();
        app.UseMiddleware<OrderApi.Middleware.CorrelationIdMiddleware>();
        app.UseMiddleware<OrderApi.Middleware.IdempotencyMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? string.Empty);
            };
        });
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.UseOutputCache();
        app.UseRequestTimeouts();
    }

    /// <summary>
    /// Maps all endpoint routes — features, health checks, and API documentation.
    /// All endpoints (features and infrastructure) are discovered and registered dynamically via <see cref="EndpointExtensions.MapEndpoints"/>.
    /// </summary>
    /// <param name="app">The configured web application to map routes onto.</param>
    public static void MapApiEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapEndpoints();
    }
}
