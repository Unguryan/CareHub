using CareHub.Laboratory.Consumers;
using CareHub.Laboratory.Data;
using CareHub.Laboratory.Endpoints;
using CareHub.Laboratory.Events;
using CareHub.Laboratory.Seed;
using CareHub.Laboratory.Services;
using CareHub.Shared.AspNetCore;
using CareHub.Shared.AspNetCore.Authentication;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

builder.Services.AddDbContext<LaboratoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Laboratory")));

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
builder.Services.AddScoped<LaboratoryEventPublisher>();
builder.Services.AddScoped<LabOrderService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Configuration.SeedDemoData())
{
    using (var scope = app.Services.CreateScope())
    {
        await LaboratorySeedData.SeedAsync(scope.ServiceProvider);
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapLabOrderEndpoints();
app.MapLaboratoryInternalEndpoints(app.Configuration);

app.Run();

public partial class Program { }
