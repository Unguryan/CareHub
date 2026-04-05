using Microsoft.Extensions.Configuration;

namespace CareHub.Shared.AspNetCore;

public static class CareHubConfiguration
{
    public const string SeedDemoDataKey = "CareHub:SeedDemoData";

    /// <summary>
    /// When true (default), services may insert demo users, patients, schedules, etc.
    /// When false, only required infrastructure runs (e.g. Identity OIDC clients and roles).
    /// </summary>
    public static bool SeedDemoData(this IConfiguration configuration) =>
        configuration.GetValue(SeedDemoDataKey, defaultValue: true);
}
