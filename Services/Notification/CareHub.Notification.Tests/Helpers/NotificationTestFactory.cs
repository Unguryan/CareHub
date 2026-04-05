using CareHub.Notification.Clients;
using CareHub.Notification.Consumers;
using CareHub.Notification.Data;
using CareHub.Notification.Messaging;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CareHub.Notification.Tests.Helpers;

public class NotificationTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public RecordingTelegramNotificationSender Telegram { get; } = new();

    public TestIdentityInternalClient Identity { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<NotificationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<NotificationDbContext>>();

            services.AddDbContext<NotificationDbContext>(options =>
                options.UseSqlite(_connection));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.EnsureCreated();

            services.RemoveAll<IIdentityInternalClient>();
            services.AddSingleton<IIdentityInternalClient>(Identity);

            services.RemoveAll<ITelegramNotificationSender>();
            services.AddSingleton<ITelegramNotificationSender>(Telegram);

            services.RemoveAll<IBusControl>();
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<AppointmentCreatedConsumer>();
                x.AddConsumer<AppointmentCancelledConsumer>();
                x.AddConsumer<AppointmentRescheduledConsumer>();
                x.AddConsumer<InvoiceGeneratedConsumer>();
                x.AddConsumer<LabResultReadyConsumer>();
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
