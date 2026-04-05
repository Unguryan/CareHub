namespace CareHub.Identity.Internal;

public record TelegramUserResponse(long TelegramChatId);

public record UserBranchRoleRow(Guid UserId, long? TelegramChatId);

public record LinkTelegramRequest(long TelegramUserId, string PhoneNumber, string? TelegramUsername);

public record RequestOtpRequest(Guid UserId);

public record ValidateOtpRequest(Guid UserId, string Code);
