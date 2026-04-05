using CareHub.Document.Consumers;
using CareHub.Document.Data;
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

namespace CareHub.Document.Tests.Helpers;

public class DocumentTestFactory : WebApplicationFactory<Program>
{
    public static readonly Guid DefaultUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid DefaultBranchId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "carehub-doc-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        Directory.CreateDirectory(_storageRoot);

        builder.UseEnvironment("Test");
        builder.UseSetting("ConnectionStrings:Document", "DataSource=:memory:");
        builder.UseSetting("DocumentStorage:RootPath", _storageRoot);
        builder.UseSetting("Document:UseLaboratoryInternalApi", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DocumentDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DocumentDbContext>>();

            services.AddDbContext<DocumentDbContext>(options =>
                options.UseSqlite(_connection));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<DocumentDbContext>().Database.EnsureCreated();

            services.RemoveAll<IBusControl>();
            services.AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<InvoiceGeneratedConsumer>();
                cfg.AddConsumer<LabResultReadyConsumer>();
            });

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
        if (disposing)
        {
            _connection.Dispose();
            try
            {
                if (Directory.Exists(_storageRoot))
                    Directory.Delete(_storageRoot, recursive: true);
            }
            catch
            {
                // best-effort cleanup of temp files
            }
        }
    }
}
