using CareHub.Audit.Consumers;
using CareHub.Audit.Data;
using CareHub.Audit.Endpoints;
using CareHub.Audit.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Audit")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserLoggedInConsumer, UserLoggedInConsumerDefinition>();
    x.AddConsumer<UserLoggedOutConsumer, UserLoggedOutConsumerDefinition>();
    x.AddConsumer<PatientCreatedConsumer, PatientCreatedConsumerDefinition>();
    x.AddConsumer<PatientUpdatedConsumer, PatientUpdatedConsumerDefinition>();
    x.AddConsumer<AppointmentCreatedConsumer, AppointmentCreatedConsumerDefinition>();
    x.AddConsumer<AppointmentCancelledConsumer, AppointmentCancelledConsumerDefinition>();
    x.AddConsumer<AppointmentRescheduledConsumer, AppointmentRescheduledConsumerDefinition>();
    x.AddConsumer<AppointmentCompletedConsumer, AppointmentCompletedConsumerDefinition>();
    x.AddConsumer<InvoiceGeneratedConsumer, InvoiceGeneratedConsumerDefinition>();
    x.AddConsumer<PaymentCompletedConsumer, PaymentCompletedConsumerDefinition>();
    x.AddConsumer<RefundIssuedConsumer, RefundIssuedConsumerDefinition>();

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
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Identity:Authority"];
        options.Audience = "api";
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<AuditLogWriter>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuditLogEndpoints();

app.Run();

public partial class Program { }
