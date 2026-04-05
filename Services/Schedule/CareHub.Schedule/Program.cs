using CareHub.Schedule.Data;
using CareHub.Schedule.Endpoints;
using CareHub.Schedule.Seed;
using CareHub.Schedule.Services;
using CareHub.Shared.AspNetCore;
using CareHub.Shared.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ScheduleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Schedule")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddCareHubResourceServerJwtBearer(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddScoped<ScheduleService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Configuration.SeedDemoData())
{
    using (var scope = app.Services.CreateScope())
    {
        await ScheduleSeedData.SeedAsync(scope.ServiceProvider);
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapScheduleEndpoints();

app.Run();

public partial class Program { }
