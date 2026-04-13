using OrderApi.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilog();
    builder.AddPersistence();
    builder.AddApplicationServices();
    builder.AddApiInfrastructure();

    var app = builder.Build();
    await app.InitializeDatabaseAsync();

    app.UseApiMiddleware();
    app.MapApiEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
