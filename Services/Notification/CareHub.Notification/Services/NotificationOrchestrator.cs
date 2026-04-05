using CareHub.Notification.Clients;
using CareHub.Notification.Data;
using CareHub.Notification.Messaging;
using CareHub.Notification.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Notification.Services;

public sealed class NotificationOrchestrator(
    NotificationDbContext db,
    IIdentityInternalClient identity,
    ITelegramNotificationSender telegram,
    SignalRNotificationPublisher signalR,
    ILogger<NotificationOrchestrator> log) : INotificationOrchestrator
{
    public async Task HandleAsync(
        string dedupeKey,
        NotificationKind kind,
        IReadOnlyList<Guid> userIds,
        NotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        var distinct = userIds.Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            db.NotificationDedupes.Add(new NotificationDedupe
            {
                Id = Guid.NewGuid(),
                DedupeKey = dedupeKey,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            return;
        }

        var dto = new ClientNotificationDto(
            kind.ToString(),
            payload.Title,
            payload.Body,
            payload.OccurredAt,
            payload.EntityType,
            payload.EntityId);

        var telegramText = $"{payload.Title}\n{payload.Body}";

        foreach (var userId in distinct)
        {
            await DeliverSignalRAsync(userId, dto, dedupeKey, kind, cancellationToken);
            await DeliverTelegramAsync(userId, telegramText, dedupeKey, kind, cancellationToken);
        }
    }

    private async Task DeliverSignalRAsync(
        Guid userId,
        ClientNotificationDto dto,
        string dedupeKey,
        NotificationKind kind,
        CancellationToken ct)
    {
        try
        {
            await signalR.PublishToUserAsync(userId, dto, ct);
            await LogAsync(userId, kind, "SignalR", true, null, dto.Body, dedupeKey, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "SignalR notification failed for user {UserId}", userId);
            await LogAsync(userId, kind, "SignalR", false, ex.Message, dto.Body, dedupeKey, ct);
        }
    }

    private async Task DeliverTelegramAsync(
        Guid userId,
        string text,
        string dedupeKey,
        NotificationKind kind,
        CancellationToken ct)
    {
        long? chatId;
        try
        {
            chatId = await identity.GetTelegramChatIdAsync(userId, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Identity lookup failed for user {UserId}", userId);
            await LogAsync(userId, kind, "Telegram", false, ex.Message, text, dedupeKey, ct);
            return;
        }

        if (chatId is null)
        {
            log.LogInformation("Skip Telegram for user {UserId} (not linked)", userId);
            await LogAsync(userId, kind, "Telegram", false, "not linked", text, dedupeKey, ct);
            return;
        }

        try
        {
            await telegram.SendTextAsync(chatId.Value, text, ct);
            await LogAsync(userId, kind, "Telegram", true, null, text, dedupeKey, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Telegram send failed for user {UserId}", userId);
            await LogAsync(userId, kind, "Telegram", false, ex.Message, text, dedupeKey, ct);
        }
    }

    private async Task LogAsync(
        Guid userId,
        NotificationKind kind,
        string channel,
        bool success,
        string? error,
        string? summary,
        string dedupeKey,
        CancellationToken ct)
    {
        db.NotificationDeliveries.Add(new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Kind = kind.ToString(),
            Channel = channel,
            TargetUserId = userId,
            Success = success,
            ErrorMessage = error,
            PayloadSummary = summary is { Length: > 500 } ? summary[..500] : summary,
            DedupeKey = dedupeKey
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to persist notification delivery log");
        }
    }
}
