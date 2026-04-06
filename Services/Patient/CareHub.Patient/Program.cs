using CareHub.Patient.Data;
using CareHub.Patient.Endpoints;
using CareHub.Patient.Events;
using CareHub.Patient.Seed;
using CareHub.Patient.Services;
using CareHub.Shared.AspNetCore;
using CareHub.Shared.AspNetCore.Authentication;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<PatientDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Patient")));

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]!);
            h.Password(builder.Configuration["RabbitMq:Password"]!);
        });
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddCareHubResourceServerJwtBearer(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddScoped<PatientEventPublisher>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Test"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Configuration.SeedDemoData())
{
    using (var scope = app.Services.CreateScope())
    {
        await PatientSeedData.SeedAsync(scope.ServiceProvider);
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapPatientEndpoints();

app.Run();

public partial class Program { }
