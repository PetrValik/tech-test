using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderApi.Common.Behaviors;
using OrderApi.Common.Endpoints;
using OrderApi.Common.Events;
using OrderApi.Exceptions;
using OrderApi.Infrastructure;
using OrderApi.Middleware;
using OrderApi.Services;
using Scalar.AspNetCore;
using Serilog;
using System.IO.Compression;

namespace OrderApi.Extensions;

/// <summary>
/// Organises the application bootstrap into focused, self-documenting methods.
/// Each method groups a single responsibility so Program.cs reads like an outline.
/// </summary>
public static class ServiceCollectionExtensions
{
    // ── Logging ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the default logging with Serilog, reading configuration from appsettings.
    /// Enriches every log entry with the service name and deployment environment.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    public static void AddSerilog(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Configuration["Otel:ServiceName"] ?? "order-api-v2";
        var environment = builder.Environment.EnvironmentName;

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("Environment", environment));
    }

    // ── Persistence ──────────────────────────────────────────────────────────

    /// <summary>
    /// Registers the EF Core <see cref="OrderContext"/> with Pomelo MySQL and a health check.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    public static void AddPersistence(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration["OrderConnectionString"]
            ?? throw new InvalidOperationException("OrderConnectionString is not configured.");

        builder.Services.AddDbContext<OrderContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.Parse(builder.Configuration["MySqlVersion"] ?? "5.7.0-mysql"),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)));

        var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
        var healthCheckBuilder = builder.Services.AddHealthChecks()
            .AddDbContextCheck<OrderContext>();
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthCheckBuilder.AddRedis(redisConnectionString, name: "redis-cache");
        }
    }

    // ── Application services ─────────────────────────────────────────────────

    /// <summary>
    /// Registers FluentValidation, MediatR (with pipeline behaviors),
    /// the background cleanup job, the global exception handler,
    /// and the domain event publisher.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<Program>();
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddHostedService<StaleOrderCleanupService>();
        builder.Services.AddHostedService<IdempotencyCleanupService>();

        // Domain event publisher — null implementation for local dev, swap for real broker in production.
        // See IOrderEventPublisher.cs for instructions on wiring Azure Service Bus / AWS SQS.
        builder.Services.AddSingleton<IOrderEventPublisher, NullOrderEventPublisher>();
    }

    // ── Cross-cutting API infrastructure ─────────────────────────────────────

    /// <summary>
    /// Registers all concrete <see cref="OrderApi.Common.Endpoints.IEndpoint"/> implementations
    /// found in the assembly, OpenAPI/Scalar, JWT authentication, response compression, CORS,
    /// rate limiting, request timeouts, output caching, OpenTelemetry, and API versioning.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    public static void AddApiInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();

        // Endpoint discovery — scans the assembly and registers every IEndpoint
        // implementation so MapEndpoints() can resolve and wire them at startup.
        builder.Services.AddEndpoints();

        // API versioning — allows non-breaking evolution of the API.
        // Current version: 1.0 at /api/v1/
        // When introducing a v2: add new handlers and register new routes under /api/v2/
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"));
        });

        // Request timeouts — 30 s default
        builder.Services.AddRequestTimeouts(options =>
            options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

        ConfigureCompression(builder);

        var jwtAuthority = builder.Configuration["Jwt:Authority"];
        ConfigureAuthentication(builder, jwtAuthority);

        ConfigureCors(builder);
        ConfigureRateLimiting(builder);
        ConfigureOutputCache(builder);
        ConfigureTelemetry(builder);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Registers Brotli and Gzip response compression providers with
    /// the <see cref="CompressionLevel.Fastest"/> level so that large
    /// list responses are compressed with minimal CPU overhead.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    private static void ConfigureCompression(WebApplicationBuilder builder)
    {
        // Response compression (Brotli + Gzip)
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        builder.Services.Configure<BrotliCompressionProviderOptions>(compressionOptions => compressionOptions.Level = CompressionLevel.Fastest);
        builder.Services.Configure<GzipCompressionProviderOptions>(compressionOptions => compressionOptions.Level = CompressionLevel.Fastest);
    }

    /// <summary>
    /// Configures JWT Bearer authentication and scope-based authorization policies.
    /// When <paramref name="jwtAuthority"/> is <see langword="null"/> or empty the
    /// fallback policy is cleared so local development works without an identity provider.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <param name="jwtAuthority">The authority URL of the identity provider, or <see langword="null"/> to disable authentication enforcement.</param>
    private static void ConfigureAuthentication(WebApplicationBuilder builder, string? jwtAuthority)
    {
        ConfigureJwtBearer(builder, jwtAuthority);
        ConfigureAuthorizationPolicies(builder, jwtAuthority);
    }

    /// <summary>
    /// Registers the JWT Bearer authentication scheme.
    /// When <paramref name="jwtAuthority"/> is <see langword="null"/>, token validation
    /// is configured but no authority is set, allowing unauthenticated requests in
    /// local development environments.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <param name="jwtAuthority">The authority URL of the identity provider, or <see langword="null"/> for local development without an identity provider.</param>
    private static void ConfigureJwtBearer(WebApplicationBuilder builder, string? jwtAuthority)
    {
        // Configure Jwt:Authority + Jwt:Audience via environment variables or Azure Key Vault.
        // Leave Jwt:Authority empty to allow unauthenticated requests (local dev without IDP).
        //
        // Azure AD example authority: https://login.microsoftonline.com/{tenantId}/v2.0
        // AWS Cognito example: https://cognito-idp.{region}.amazonaws.com/{userPoolId}
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = jwtAuthority;
                options.Audience = builder.Configuration["Jwt:Audience"];
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            });
    }

    /// <summary>
    /// Adds scope-based authorization policies for reading and writing orders.
    /// When <paramref name="jwtAuthority"/> is <see langword="null"/> the fallback
    /// policy is cleared so all requests are allowed through during local development.
    /// In all other environments the default policy is used as the fallback,
    /// requiring every request to be authenticated.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <param name="jwtAuthority">The authority URL of the identity provider, or <see langword="null"/> to allow unauthenticated requests (development only).</param>
    private static void ConfigureAuthorizationPolicies(WebApplicationBuilder builder, string? jwtAuthority)
    {
        builder.Services.AddAuthorization(options =>
        {
            // Scope-based policies for fine-grained access control.
            options.AddPolicy("ReadOrders", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "orders:read"));
            options.AddPolicy("WriteOrders", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "orders:write"));

            if (string.IsNullOrWhiteSpace(jwtAuthority))
            {
                // No IDP configured — fall back to allowing all authenticated-or-anonymous requests.
                // IMPORTANT: In production always configure Jwt:Authority.
                if (!builder.Environment.IsDevelopment())
                {
                    // Log startup warning — cannot use ILogger here (not yet built), so write to console.
                    Console.Error.WriteLine(
                        "WARNING: Jwt:Authority is not configured. All endpoints are publicly accessible. " +
                        "Set Jwt:Authority in production to require authentication.");
                }
                options.FallbackPolicy = null;
            }
            else
            {
                // Deny all unauthenticated requests by default.
                options.FallbackPolicy = options.DefaultPolicy;
            }
        });
    }

    /// <summary>
    /// Registers the CORS default policy.
    /// In development, all origins are allowed for convenience.
    /// In all other environments, Cors:AllowedOrigins must be configured;
    /// an <see cref="InvalidOperationException"/> is thrown at startup if it is missing.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Cors:AllowedOrigins is not configured outside of the development environment.
    /// </exception>
    private static void ConfigureCors(WebApplicationBuilder builder)
    {
        // CORS — configured origins in non-development, permissive only for local dev.
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins is { Length: > 0 })
                {
                    policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
                    return;
                }

                if (builder.Environment.IsDevelopment())
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                    return;
                }

                throw new InvalidOperationException(
                    "CORS is not configured. Set Cors:AllowedOrigins for non-development environments.");
            }));
    }

    /// <summary>
    /// Registers two fixed-window rate limiter policies:
    /// <list type="bullet">
    ///   <item><description>fixed — 100 requests per minute, applied to all order endpoints.</description></item>
    ///   <item><description>expensive — 10 requests per minute, applied to resource-intensive endpoints such as search and monthly profit.</description></item>
    /// </list>
    /// Rejected requests receive a 429 Too Many Requests response.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    private static void ConfigureRateLimiting(WebApplicationBuilder builder)
    {
        // Fixed-window rate limiter — 100 requests/minute (general)
        // Expensive rate limiter — 10 requests/minute (search, profit)
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("fixed", limiter =>
            {
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.PermitLimit = 100;
                limiter.QueueLimit = 0;
            });
            options.AddFixedWindowLimiter("expensive", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.AutoReplenishment = true;
            });
        });
    }

    /// <summary>
    /// Registers output caching with a named orders policy that expires after one minute.
    /// When a Redis connection string is configured, the distributed Redis store is used so
    /// all replicas share the same cache. Without Redis, the default in-memory store is used,
    /// which is suitable for single-instance local development.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    private static void ConfigureOutputCache(WebApplicationBuilder builder)
    {
        // Output caching — Redis for production clusters (each replica shares the cache),
        // fallback to in-memory for local development (no Redis needed).
        var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            builder.Services.AddStackExchangeRedisOutputCache(options =>
                options.Configuration = redisConnectionString);
        }

        builder.Services.AddOutputCache(options =>
        {
            options.AddBasePolicy(policy => policy.NoCache());
            options.AddPolicy("orders", policy => policy.Tag("orders").Expire(TimeSpan.FromMinutes(1)));
        });
    }

    /// <summary>
    /// Configures OpenTelemetry tracing and metrics with ASP.NET Core and HTTP client
    /// instrumentation. Health check endpoints are excluded from traces to reduce noise.
    /// Uses the console exporter for development and staging; swap for a production
    /// exporter (Azure Monitor, OTLP) by following the inline comments.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    private static void ConfigureTelemetry(WebApplicationBuilder builder)
    {
        // OpenTelemetry — W3C traceparent propagation with console exporter for dev/staging.
        // For production swap the console exporter:
        //   Azure Monitor: .AddAzureMonitorTraceExporter(o => o.ConnectionString = builder.Configuration["Otel:AzureMonitorConnectionString"])
        //   AWS X-Ray / Jaeger: .AddOtlpExporter(o => o.Endpoint = new Uri(builder.Configuration["Otel:OtlpEndpoint"]))
        var serviceName = builder.Configuration["Otel:ServiceName"] ?? "order-api-v2";
        var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        var environment = builder.Environment.EnvironmentName;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.RecordException = true;
                    opts.Filter = httpContext =>
                        // Exclude health check endpoints from traces to reduce noise.
                        !httpContext.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddConsoleExporter());
    }
}
