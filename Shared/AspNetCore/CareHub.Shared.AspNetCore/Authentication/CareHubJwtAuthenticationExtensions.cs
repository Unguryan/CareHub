using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CareHub.Shared.AspNetCore.Authentication;

public static class CareHubJwtAuthenticationExtensions
{
    public const string ApiAudience = "api";

    /// <summary>
    /// JWT validation for downstream APIs (resource server): validates issuer and api audience.
    /// </summary>
    public static AuthenticationBuilder AddCareHubResourceServerJwtBearer(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureOptions = null)
    {
        return builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = configuration["Identity:Authority"];
            options.Audience = ApiAudience;
            options.RequireHttpsMetadata = false;
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// JWT validation at the gateway: issuer validated; audience not enforced (token is for backend APIs).
    /// </summary>
    public static AuthenticationBuilder AddCareHubGatewayJwtBearer(
        this AuthenticationBuilder builder,
        IConfiguration configuration)
    {
        return builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = configuration["Identity:Authority"];
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = true,
            };
        });
    }
}
