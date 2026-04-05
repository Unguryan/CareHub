using CareHub.Identity.Data;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace CareHub.Identity.Tests.Helpers;

public class IdentityTestFactory : WebApplicationFactory<Program>
{
    // Keep the connection open so the in-memory SQLite database persists for the test run
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core registrations for this context (provider + options config)
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();

            services.AddDbContext<IdentityDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseOpenIddict<Guid>();
            });

            // Create schema via a temporary service provider
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.Database.EnsureCreated();

            // Replace MassTransit with in-memory test harness (no RabbitMQ needed)
            services.RemoveAll<IBusControl>();
            services.AddMassTransitTestHarness();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
