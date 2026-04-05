using CareHub.Document.Clients;
using CareHub.Document.Consumers;
using CareHub.Document.Data;
using CareHub.Document.Endpoints;
using CareHub.Document.Options;
using CareHub.Document.Pdf;
using CareHub.Document.Storage;
using CareHub.Document.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

var storagePath = builder.Configuration["DocumentStorage:RootPath"];
if (string.IsNullOrWhiteSpace(storagePath))
    throw new InvalidOperationException("DocumentStorage:RootPath must be configured.");
var fullStoragePath = Path.GetFullPath(storagePath);
Directory.CreateDirectory(fullStoragePath);

builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Document")));

builder.Services.Configure<DocumentStorageOptions>(builder.Configuration.GetSection(DocumentStorageOptions.SectionName));
builder.Services.Configure<DocumentFeatureOptions>(builder.Configuration.GetSection(DocumentFeatureOptions.SectionName));
builder.Services.Configure<LaboratoryInternalOptions>(builder.Configuration.GetSection(LaboratoryInternalOptions.SectionName));

builder.Services.AddSingleton<IDocumentStorage>(_ => new LocalDocumentStorage(fullStoragePath));

builder.Services.AddScoped<IInvoicePdfRenderer, QuestInvoicePdfRenderer>();
builder.Services.AddScoped<ILabResultPdfRenderer, QuestLabResultPdfRenderer>();
builder.Services.AddScoped<DocumentOrchestrator>();

var useLabInternal = builder.Configuration.GetValue("Document:UseLaboratoryInternalApi", true);
if (useLabInternal)
{
    builder.Services.AddHttpClient<ILaboratoryInternalClient, HttpLaboratoryInternalClient>((sp, client) =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<LaboratoryInternalOptions>>().CurrentValue;
        var baseUrl = opts.BaseUrl?.TrimEnd('/');
        if (!string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}
else
{
    builder.Services.AddSingleton<ILaboratoryInternalClient, NullLaboratoryInternalClient>();
}

builder.Services.AddMassTransit(x =>
{
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Identity:Authority"];
        options.Audience = "api";
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Test"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
        await db.Database.MigrateAsync();
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapDocumentEndpoints();

app.Run();

public partial class Program { }
