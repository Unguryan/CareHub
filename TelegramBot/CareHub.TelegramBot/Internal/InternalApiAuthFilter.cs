namespace CareHub.TelegramBot.Internal;

public sealed class InternalApiAuthFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = configuration["InternalApi:SharedSecret"];
        if (string.IsNullOrEmpty(expected))
            return Results.Problem("Internal API is not configured.", statusCode: 503);

        if (!context.HttpContext.Request.Headers.TryGetValue("X-CareHub-Internal-Key", out var supplied)
            || supplied.Count != 1
            || !string.Equals(supplied[0], expected, StringComparison.Ordinal))
            return Results.Unauthorized();

        return await next(context);
    }
}
