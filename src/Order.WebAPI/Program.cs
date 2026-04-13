using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Order.WebAPI;

/// <summary>
/// Application entry point. Builds and runs the web host.
/// </summary>
public class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the host.</param>
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    /// <summary>
    /// Creates and configures the generic host with Kestrel and the Startup class.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to the host builder.</param>
    /// <returns>A configured IHostBuilder ready to be built and run.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
