using CareHub.Identity.Auth;
using CareHub.Identity.Data;
using CareHub.Identity.Events;
using CareHub.Identity.Internal;
using CareHub.Identity.Models;
using CareHub.Identity.Seed;
using CareHub.Identity.Services;
using CareHub.Shared.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using StackExchange.Redis;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

// Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Identity"));
    options.UseOpenIddict<Guid>();
});

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = false;
    options.ClaimsIdentity.UserNameClaimType = Claims.Name;
    options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
    options.ClaimsIdentity.RoleClaimType = Claims.Role;
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<IdentityDbContext>()
               .ReplaceDefaultEntities<Guid>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.SetUserinfoEndpointUris("/connect/userinfo");

        options.AllowPasswordFlow()
               .AllowClientCredentialsFlow()
               .AllowRefreshTokenFlow();

        options.RegisterScopes(
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.OfflineAccess,
            "api");

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        var aspNetCoreBuilder = options.UseAspNetCore();

        if (!builder.Environment.IsProduction())
            aspNetCoreBuilder.DisableTransportSecurityRequirement();

        // Register grant handlers
        options.AddEventHandler(PasswordGrantHandler.Descriptor);
        options.AddEventHandler(ClientCredentialsGrantHandler.Descriptor);
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

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

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IOtpService, MemoryOtpService>();
    builder.Services.AddSingleton<ITelegramBotRelay, NoOpTelegramBotRelay>();
}
else
{
    var redisConnection = builder.Configuration["Redis:ConnectionString"];
    if (!string.IsNullOrEmpty(redisConnection))
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));
        builder.Services.AddSingleton<IOtpService, RedisOtpService>();
    }
    else
    {
        builder.Services.AddSingleton<IOtpService, MemoryOtpService>();
    }

    var botBase = builder.Configuration["TelegramBot:InternalBaseUrl"];
    if (!string.IsNullOrEmpty(botBase))
    {
        builder.Services.AddHttpClient<ITelegramBotRelay, TelegramBotRelay>(client =>
        {
            client.BaseAddress = new Uri(botBase.TrimEnd('/') + "/");
        });
    }
    else
    {
        builder.Services.AddSingleton<ITelegramBotRelay, NoOpTelegramBotRelay>();
    }
}

builder.Services.AddScoped<UserEventPublisher>();
builder.Services.AddHealthChecks();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Seed infrastructure (always) and optional demo users
using (var scope = app.Services.CreateScope())
{
    await IdentitySeedData.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthDiscoveryEndpoints();
app.MapInternalEndpoints();

app.Run();

public partial class Program { }
