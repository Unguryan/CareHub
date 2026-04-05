using CareHub.Laboratory.Consumers;
using CareHub.Laboratory.Data;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CareHub.Laboratory.Tests.Helpers;

public class LaboratoryTestFactory : WebApplicationFactory<Program>
{
    public static readonly Guid DefaultUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid DefaultBranchId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LaboratoryDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<LaboratoryDbContext>>();

            services.AddDbContext<LaboratoryDbContext>(options =>
                options.UseSqlite(_connection));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<LaboratoryDbContext>().Database.EnsureCreated();

            services.RemoveAll<IBusControl>();
            services.AddMassTransitTestHarness(cfg => { cfg.AddConsumer<AppointmentCompletedConsumer>(); });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.PostConfigureAll<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
