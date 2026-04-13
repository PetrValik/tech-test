using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderApi.Infrastructure;

/// <summary>
/// Design-time DbContext factory used by EF tooling (migrations add/update/list),
/// so migrations can be generated without bootstrapping the full web host.
/// </summary>
public sealed class OrderContextFactory : IDesignTimeDbContextFactory<OrderContext>
{
    /// <summary>
    /// Creates a configured <see cref="OrderContext"/> using appsettings and environment variables.
    /// </summary>
    public OrderContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["OrderConnectionString"]
            ?? "server=localhost;port=3306;database=orders;user=order-service;password=";

        var mySqlVersion = configuration["MySqlVersion"] ?? "5.7.0-mysql";

        var optionsBuilder = new DbContextOptionsBuilder<OrderContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.Parse(mySqlVersion));

        return new OrderContext(optionsBuilder.Options);
    }
}
