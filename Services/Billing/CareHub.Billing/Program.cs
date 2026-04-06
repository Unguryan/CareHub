using CareHub.Billing.Consumers;
using CareHub.Billing.Data;
using CareHub.Billing.Endpoints;
using CareHub.Billing.Events;
using CareHub.Billing.Seed;
using CareHub.Billing.Services;
using CareHub.Shared.AspNetCore;
using CareHub.Shared.AspNetCore.Authentication;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Billing")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AppointmentCompletedConsumer>();
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
builder.Services.AddScoped<BillingEventPublisher>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Test"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Configuration.SeedDemoData())
{
    using (var scope = app.Services.CreateScope())
    {
        await BillingSeedData.SeedAsync(scope.ServiceProvider);
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapInvoiceEndpoints();

app.Run();

public partial class Program { }
