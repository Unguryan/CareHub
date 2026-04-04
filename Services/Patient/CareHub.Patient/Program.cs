using CareHub.Patient.Data;
using CareHub.Patient.Endpoints;
using CareHub.Patient.Events;
using CareHub.Patient.Seed;
using CareHub.Patient.Services;
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

// Authentication — JWT validated independently per service
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Identity:Authority"];
        options.Audience = "api";
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<PatientEventPublisher>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    await PatientSeedData.SeedAsync(scope.ServiceProvider);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapPatientEndpoints();

app.Run();

public partial class Program { }
