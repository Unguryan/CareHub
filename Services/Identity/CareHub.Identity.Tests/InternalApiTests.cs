using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareHub.Identity.Models;
using CareHub.Identity.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Identity.Tests;

[CollectionDefinition("SequentialIdentityInternal", DisableParallelization = true)]
public sealed class SequentialIdentityInternalDefinition;

[Collection("SequentialIdentityInternal")]
public class InternalApiTests : IClassFixture<IdentityTestFactory>
{
    private const string InternalKey = "dev-internal-key-change-in-production";
    private readonly IdentityTestFactory _factory;

    public InternalApiTests(IdentityTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_telegram_without_key_returns_401()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync($"/internal/users/{Guid.NewGuid()}/telegram");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_telegram_with_invalid_key_returns_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CareHub-Internal-Key", "wrong");
        var res = await client.GetAsync($"/internal/users/{Guid.NewGuid()}/telegram");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_telegram_returns_404_when_not_linked()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CareHub-Internal-Key", InternalKey);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("+380000000000");
        user.Should().NotBeNull();
        user!.TelegramChatId = null;
        await userManager.UpdateAsync(user);

        var res = await client.GetAsync($"/internal/users/{user.Id}/telegram");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_telegram_returns_chat_id_when_linked()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CareHub-Internal-Key", InternalKey);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("+380000000000");
        user.Should().NotBeNull();
        user!.TelegramChatId = 424242;
        await userManager.UpdateAsync(user);

        try
        {
            var res = await client.GetAsync($"/internal/users/{user.Id}/telegram");
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var doc = await res.Content.ReadFromJsonAsync<JsonElement>();
            doc.GetProperty("telegramChatId").GetInt64().Should().Be(424242);
        }
        finally
        {
            user.TelegramChatId = null;
            await userManager.UpdateAsync(user);
        }
    }

    [Fact]
    public async Task Request_otp_succeeds_when_telegram_linked_and_relay_is_noop()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CareHub-Internal-Key", InternalKey);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("+380000000000");
        user.Should().NotBeNull();
        user!.TelegramChatId = 999001;
        await userManager.UpdateAsync(user);

        try
        {
            var res = await client.PostAsJsonAsync("/internal/telegram/request-otp", new { userId = user.Id });
            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            user.TelegramChatId = null;
            await userManager.UpdateAsync(user);
        }
    }
}
