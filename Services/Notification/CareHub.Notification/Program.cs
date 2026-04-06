using CareHub.Notification.Clients;
using CareHub.Notification.Consumers;
using CareHub.Notification.Data;
using CareHub.Notification.Hubs;
using CareHub.Notification.Messaging;
using CareHub.Notification.Seed;
using CareHub.Notification.Services;
using CareHub.Shared.AspNetCore;
using CareHub.Shared.AspNetCore.Authentication;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Notification")));

builder.Services.AddSignalR();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddCareHubResourceServerJwtBearer(builder.Configuration, options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessTokenValues = context.Request.Query["access_token"];
                var accessToken = accessTokenValues.Count > 0 ? accessTokenValues[0] : null;
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Notification.Jwt");
                logger.LogWarning(
                    context.Exception,
                    "JWT authentication failed for {Path}",
                    context.HttpContext.Request.Path);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Notification.Jwt");
                logger.LogWarning(
                    "JWT challenge on {Path}. Error: {Error}. Description: {Description}",
                    context.HttpContext.Request.Path,
                    context.Error,
                    context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var telegramToken = builder.Configuration["Telegram:BotToken"];
if (!string.IsNullOrWhiteSpace(telegramToken))
{
    builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(telegramToken));
    builder.Services.AddSingleton<ITelegramNotificationSender, TelegramNotificationSender>();
}
else
{
    builder.Services.AddSingleton<ITelegramNotificationSender, NoOpTelegramNotificationSender>();
}

builder.Services.AddHttpClient<IIdentityInternalClient, IdentityInternalClient>(client =>
{
    var baseUrl = builder.Configuration["Identity:InternalApiBaseUrl"]
                  ?? builder.Configuration["Identity:Authority"];
    if (!string.IsNullOrEmpty(baseUrl))
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    var key = builder.Configuration["Identity:InternalApiKey"];
    if (!string.IsNullOrEmpty(key))
        client.DefaultRequestHeaders.Add("X-CareHub-Internal-Key", key);
});

builder.Services.AddScoped<SignalRNotificationPublisher>();
builder.Services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AppointmentCreatedConsumer, AppointmentCreatedConsumerDefinition>();
    x.AddConsumer<AppointmentCancelledConsumer, AppointmentCancelledConsumerDefinition>();
    x.AddConsumer<AppointmentRescheduledConsumer, AppointmentRescheduledConsumerDefinition>();
    x.AddConsumer<InvoiceGeneratedConsumer, InvoiceGeneratedConsumerDefinition>();
    x.AddConsumer<LabResultReadyConsumer, LabResultReadyConsumerDefinition>();

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

builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Test"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Configuration.SeedDemoData())
{
    using (var scope = app.Services.CreateScope())
    {
        await NotificationSeedData.SeedAsync(scope.ServiceProvider);
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

public partial class Program { }
