using System;
using System.IO.Compression;
using System.Linq;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Data;
using Order.Data.Repositories;
using Order.Service;
using Order.WebAPI.Middleware;

namespace Order.WebAPI;

/// <summary>
/// Configures services and the HTTP request pipeline for the Order API.
/// </summary>
public class Startup
{
    /// <summary>
    /// Initialises Startup with the application configuration and hosting environment.
    /// </summary>
    /// <param name="configuration">Configuration provided by the host (appsettings, env vars, etc.).</param>
    /// <param name="environment">Hosting environment used for environment-specific configuration.</param>
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Application configuration used to read connection strings and other settings.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// Hosting environment used to conditionally enable features (e.g. auth, CORS, Swagger)
    /// and select environment-specific configuration.
    /// </summary>
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Registers services with the DI container.
    /// Called by the runtime before Configure.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        AddPersistenceServices(services);
        AddAuthenticationServices(services);

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Startup>();

        AddHealthCheckServices(services);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddControllers();
        AddSwaggerServices(services);
        AddResponseCompressionServices(services);
        AddCorsServices(services);
        AddRateLimitingServices(services);
    }

    /// <summary>
    /// Builds and configures the HTTP request pipeline.
    /// Called by the runtime after ConfigureServices.
    /// </summary>
    /// <param name="app">Application builder used to add middleware.</param>
    /// <param name="env">Provides information about the current hosting environment.</param>
    /// <param name="logger">Structured logger injected by the runtime.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (string.IsNullOrWhiteSpace(Configuration["Jwt:Authority"]))
        {
            logger.LogWarning("Jwt:Authority is not configured — authentication is disabled. Set Jwt__Authority to enable token validation.");
        }

        var corsOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (corsOrigins.Length == 0 && !env.IsDevelopment())
        {
            logger.LogWarning(
                "Cors:AllowedOrigins is not configured — all origins are permitted. " +
                "Set Cors:AllowedOrigins to restrict cross-origin requests in non-Development environments.");
        }

        app.UseExceptionHandler();
        app.UseResponseCompression();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API v1"));

        app.UseHttpsRedirection();
        app.UseCors();

        app.UseRouting();

        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            MapHealthCheckEndpoints(endpoints);
        });
    }

    /// <summary>
    /// Registers the EF Core DbContext and scoped service/repository implementations.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    private void AddPersistenceServices(IServiceCollection services)
    {
        services.AddDbContext<OrderContext>(options =>
        {
            var connectionString = Configuration["OrderConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "OrderConnectionString is not configured. Set the environment variable or add it to appsettings.json.");
            }
            var serverVersion = ServerVersion.Parse(Configuration["MySqlVersion"] ?? "8.0.21-mysql");
            options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null));
        });

        services.AddScoped<IOrderService, Order.Service.OrderService>();
        services.AddScoped<IOrderRepository, OrderRepository>();
    }

    /// <summary>
    /// Configures JWT Bearer authentication when <c>Jwt:Authority</c> is set, or installs a
    /// passthrough authorization policy in Development mode when auth is intentionally disabled.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    private void AddAuthenticationServices(IServiceCollection services)
    {
        var jwtAuthority = Configuration["Jwt:Authority"];
        if (!string.IsNullOrWhiteSpace(jwtAuthority))
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = jwtAuthority;
                    options.Audience = Configuration["Jwt:Audience"];
                    options.RequireHttpsMetadata = !_environment.IsDevelopment();
                });
        }
        else
        {
            // Auth is disabled — override default policy so [Authorize] is a no-op.
            services.AddAuthorization(options =>
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true)
                    .Build());
        }
    }

    /// <summary>
    /// Registers the EF Core DbContext health check used by the readiness probe.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    private static void AddHealthCheckServices(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<OrderContext>();
    }

    /// <summary>
    /// Registers Swagger/OpenAPI generation and wires in the XML documentation file if present.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    private static void AddSwaggerServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title       = "Order API",
                Version     = "v1",
                Description = "Order API – manages orders, order items, and monthly profit reporting."
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });
    }

    /// <summary>
    /// Configures Brotli and GZip response compression at the fastest compression level.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    private static void AddResponseCompressionServices(IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
    }

    /// <summary>
    /// Configures CORS using the allowed origins from the <c>Cors:AllowedOrigins</c> configuration section.
    /// Falls back to allowing any origin when no origins are configured.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    private void AddCorsServices(IServiceCollection services)
    {
        var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyMethod().AllowAnyHeader();
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }
            }));
    }

    /// <summary>
    /// Configures a fixed-window rate limiter keyed by authenticated user name or remote IP,
    /// allowing 100 requests per minute. Returns HTTP 429 when the limit is exceeded.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    private static void AddRateLimitingServices(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("fixed", context =>
            {
                var key = context.User.Identity?.Name
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true
                });
            });
        });
    }

    /// <summary>
    /// Maps the liveness and readiness health-check endpoints.
    /// Liveness (<c>/health/live</c>) always returns 200 with no dependency checks.
    /// Readiness (<c>/health/ready</c>) runs all registered health checks and returns 503 when unhealthy.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder provided by <c>UseEndpoints</c>.</param>
    private static void MapHealthCheckEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
    {
        // Liveness: always 200 — pod is alive if it can respond (no dependency checks).
        // Kubernetes liveness probe: GET /health/live
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (httpContext, _) =>
            {
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsJsonAsync(new { status = "Healthy" });
            }
        });

        // Readiness: checks all registered health checks (currently: database).
        // Kubernetes readiness probe: GET /health/ready
        // Returns 503 when the database is unavailable — removes pod from load balancer.
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = async (httpContext, report) =>
            {
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString() })
                });
            }
        });
    }
}
