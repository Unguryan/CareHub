using System.Text.Json;
using CareHub.Identity.Models;
using CareHub.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Identity.Internal;

public static class InternalEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static void MapInternalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/internal").AddEndpointFilter<InternalApiAuthFilter>();

        group.MapGet("/users/{userId:guid}/telegram", async Task<IResult> (
            Guid userId,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user?.TelegramChatId is null)
                return Results.NotFound();
            return Results.Json(new TelegramUserResponse(user.TelegramChatId.Value));
        });

        group.MapGet("/users/by-branch-role", async Task<IResult> (
            Guid branchId,
            string role,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var users = await userManager.GetUsersInRoleAsync(role);
            var rows = users
                .Where(u => u.BranchId == branchId)
                .Select(u => new UserBranchRoleRow(u.Id, u.TelegramChatId))
                .ToList();
            return Results.Json(rows);
        });

        group.MapPost("/telegram/link", async Task<IResult> (
            HttpRequest request,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var body = await JsonSerializer.DeserializeAsync<LinkTelegramRequest>(request.Body, JsonOptions, cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.PhoneNumber))
                return Results.BadRequest();

            var normalized = NormalizePhone(body.PhoneNumber);
            var user = await userManager.Users.FirstOrDefaultAsync(
                u => u.PhoneNumber == normalized || u.UserName == normalized, ct);
            if (user is null)
                return Results.NotFound();

            user.TelegramChatId = body.TelegramUserId;
            user.TelegramUsername = body.TelegramUsername;
            user.TelegramLinkedAt = DateTime.UtcNow;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors.Select(e => e.Description).ToArray());
            return Results.Ok();
        });

        group.MapPost("/telegram/request-otp", async Task<IResult> (
            HttpRequest request,
            UserManager<ApplicationUser> userManager,
            IOtpService otpService,
            ITelegramBotRelay telegramBotRelay,
            CancellationToken ct) =>
        {
            var body = await JsonSerializer.DeserializeAsync<RequestOtpRequest>(request.Body, JsonOptions, cancellationToken: ct);
            if (body is null || body.UserId == Guid.Empty)
                return Results.BadRequest();

            var user = await userManager.FindByIdAsync(body.UserId.ToString());
            if (user?.TelegramChatId is null)
                return Results.BadRequest("Telegram is not linked for this user.");

            if (await otpService.IsLockedOutAsync(body.UserId, ct))
                return Results.Problem("Too many failed attempts. Try again later.", statusCode: 429);

            var code = await otpService.IssueOtpAsync(body.UserId, ct);
            var message = $"Your CareHub verification code: {code}";
            await telegramBotRelay.SendTextAsync(user.TelegramChatId.Value, message, ct);
            return Results.Ok();
        });

        group.MapPost("/otp/validate", async Task<IResult> (
            HttpRequest request,
            IOtpService otpService,
            CancellationToken ct) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ValidateOtpRequest>(request.Body, JsonOptions, cancellationToken: ct);
            if (body is null || body.UserId == Guid.Empty || string.IsNullOrWhiteSpace(body.Code))
                return Results.BadRequest();

            var ok = await otpService.TryValidateOtpAsync(body.UserId, body.Code.Trim(), ct);
            return ok ? Results.Ok() : Results.Unauthorized();
        });
    }

    private static string NormalizePhone(string phone)
    {
        var trimmed = phone.Trim().Replace(" ", "", StringComparison.Ordinal);
        return trimmed.StartsWith('+') ? trimmed : "+" + trimmed.TrimStart('+');
    }
}
