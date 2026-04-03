using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CareHub.Gateway.Tests;

public class HealthCheckTests : IClassFixture<GatewayTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(GatewayTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk_WithoutAuthentication()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProtectedRoute_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/patients");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuthRoute_WithoutToken_IsNotBlocked_By401()
    {
        var response = await _client.GetAsync("/api/auth/login");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}

public class GatewayTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:Authority", "http://localhost:5001");
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Authority = null;
                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = false,
                        SignatureValidator = (token, _) =>
                            new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token)
                    };
                });
        });
    }
}
