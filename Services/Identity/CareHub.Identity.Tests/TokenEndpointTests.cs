using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareHub.Identity.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Identity.Tests;

public class TokenEndpointTests : IClassFixture<IdentityTestFactory>
{
    private readonly HttpClient _client;

    public TokenEndpointTests(IdentityTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PasswordGrant_WithValidCredentials_ReturnsAccessToken()
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "carehub-desktop",
            ["client_secret"] = "desktop-secret",
            ["username"] = "+380000000000",
            ["password"] = "Admin1234!",
            ["scope"] = "openid api",
        };

        var response = await _client.PostAsync("/connect/token",
            new FormUrlEncodedContent(form));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("token_type").GetString().Should().Be("Bearer");
    }

    [Fact]
    public async Task PasswordGrant_WithWrongPassword_Returns400()
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "carehub-desktop",
            ["client_secret"] = "desktop-secret",
            ["username"] = "+380000000000",
            ["password"] = "WrongPassword!",
            ["scope"] = "openid api",
        };

        var response = await _client.PostAsync("/connect/token",
            new FormUrlEncodedContent(form));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("invalid_grant");
    }

    [Fact]
    public async Task ClientCredentials_WithValidSecret_ReturnsAccessToken()
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "carehub-services",
            ["client_secret"] = "services-secret",
            ["scope"] = "api",
        };

        var response = await _client.PostAsync("/connect/token",
            new FormUrlEncodedContent(form));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
    }
}
