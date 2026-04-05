using CareHub.Reporting.Consumers;
using CareHub.Reporting.Data;
using CareHub.Reporting.Endpoints;
using CareHub.Reporting.Services;
using CareHub.Shared.AspNetCore.Authentication;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

builder.Services.AddDbContext<ReportingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Reporting")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReportingPatientCreatedConsumer, ReportingPatientCreatedConsumerDefinition>();
    x.AddConsumer<ReportingAppointmentCreatedConsumer, ReportingAppointmentCreatedConsumerDefinition>();
    x.AddConsumer<ReportingAppointmentCompletedConsumer, ReportingAppointmentCompletedConsumerDefinition>();
    x.AddConsumer<ReportingAppointmentCancelledConsumer, ReportingAppointmentCancelledConsumerDefinition>();
    x.AddConsumer<ReportingPaymentCompletedConsumer, ReportingPaymentCompletedConsumerDefinition>();
    x.AddConsumer<ReportingRefundIssuedConsumer, ReportingRefundIssuedConsumerDefinition>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]!);
            h.Password(builder.Configuration["RabbitMq:Password"]!);
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddCareHubResourceServerJwtBearer(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddScoped<ReportingProjectionService>();
builder.Services.AddScoped<ReportQueryService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Test"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    await db.Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapReportEndpoints();

app.Run();

public partial class Program { }
