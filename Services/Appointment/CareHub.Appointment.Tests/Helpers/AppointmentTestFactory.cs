using CareHub.Appointment.Data;
using CareHub.Appointment.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CareHub.Appointment.Tests.Helpers;

public class AppointmentTestFactory : WebApplicationFactory<Program>
{
    public static readonly Guid DefaultUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid DefaultBranchId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static readonly HttpMessageHandler ScheduleHandler = new FakeScheduleHandler();
    private static readonly HttpMessageHandler PatientHandler = new FakePatientHandler();

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppointmentDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppointmentDbContext>>();

            services.AddDbContext<AppointmentDbContext>(options =>
                options.UseSqlite(_connection));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppointmentDbContext>().Database.EnsureCreated();

            services.RemoveAll<IBusControl>();
            services.AddMassTransitTestHarness();

            services.RemoveAll<ScheduleSlotClient>();
            services.RemoveAll<PatientClient>();
            services.AddSingleton(_ => new ScheduleSlotClient(new HttpClient(ScheduleHandler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://schedule.test/")
            }));
            services.AddSingleton(_ => new PatientClient(new HttpClient(PatientHandler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://patient.test/")
            }));

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
