using CareHub.TelegramBot.Handlers;
using CareHub.TelegramBot.Identity;
using CareHub.TelegramBot.Internal;
using CareHub.TelegramBot.Workers;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

var token = builder.Configuration["Telegram:BotToken"];
if (string.IsNullOrWhiteSpace(token))
    throw new InvalidOperationException("Telegram:BotToken is required for CareHub.TelegramBot.");

builder.Services.AddSingleton(_ => new TelegramBotClient(token));
builder.Services.AddSingleton<TelegramUpdateHandler>();
builder.Services.AddHostedService<TelegramPollingWorker>();

var identityBase = builder.Configuration["Identity:InternalApiBaseUrl"]
                   ?? builder.Configuration["Identity:Authority"]
                   ?? throw new InvalidOperationException("Identity base URL is not configured.");
var internalKey = builder.Configuration["InternalApi:SharedSecret"]
                  ?? throw new InvalidOperationException("InternalApi:SharedSecret is not configured.");

builder.Services.AddHttpClient<IdentityLinkClient>(client =>
{
    client.BaseAddress = new Uri(identityBase.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("X-CareHub-Internal-Key", internalKey);
});

var app = builder.Build();

var sendTextGroup = app.MapGroup("/internal").AddEndpointFilter<InternalApiAuthFilter>();
sendTextGroup.MapPost("/telegram/send-text", async (
    SendTextRequest body,
    TelegramBotClient bot,
    CancellationToken cancellationToken) =>
{
    await bot.SendMessage(body.ChatId, body.Text, cancellationToken: cancellationToken);
    return Results.Ok();
});

app.Run();

internal sealed record SendTextRequest(long ChatId, string Text);

public partial class Program { }
