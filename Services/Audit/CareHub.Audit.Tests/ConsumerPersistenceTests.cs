using CareHub.Audit.Consumers;
using CareHub.Audit.Data;
using CareHub.Audit.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Identity;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Audit.Tests;

public class ConsumerPersistenceTests : IClassFixture<AuditTestFactory>
{
    private readonly AuditTestFactory _factory;

    public ConsumerPersistenceTests(AuditTestFactory factory) => _factory = factory;

    [Fact]
    public async Task UserLoggedInConsumer_PersistsRow()
    {
        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var userId = Guid.Parse("40000000-0000-0000-0000-000000000004");
            var branchId = AuditTestFactory.DefaultBranchId;
            await harness.Bus.Publish(new UserLoggedIn(
                userId,
                "+380000000000",
                ["Admin"],
                branchId,
                new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc)));

            var consumed = await harness.GetConsumerHarness<UserLoggedInConsumer>().Consumed.Any();
            consumed.Should().BeTrue();

            var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            var row = db.AuditLogEntries.OrderByDescending(e => e.RecordedAt).FirstOrDefault();
            row.Should().NotBeNull();
            row!.ActionType.Should().Be("identity.user.logged_in");
            row.ActorUserId.Should().Be(userId);
            row.BranchId.Should().Be(branchId);
            row.DetailsJson.Should().Contain("userId");
        }
        finally
        {
            await harness.Stop();
        }
    }
}
