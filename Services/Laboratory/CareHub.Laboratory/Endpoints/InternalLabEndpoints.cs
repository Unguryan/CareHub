using CareHub.Laboratory.Services;
using Microsoft.Extensions.Primitives;

namespace CareHub.Laboratory.Endpoints;

public static class InternalLabEndpoints
{
    public static IEndpointRouteBuilder MapLaboratoryInternalEndpoints(this IEndpointRouteBuilder app, IConfiguration config)
    {
        var expectedKey = config["LaboratoryInternal:DocumentServiceApiKey"] ?? "";
        var group = app.MapGroup("/internal");
        group.AddEndpointFilter(async (ic, next) =>
        {
            if (string.IsNullOrEmpty(expectedKey))
            {
                return Results.Json(
                    new { error = "LaboratoryInternal:DocumentServiceApiKey is not configured." },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            if (!ic.HttpContext.Request.Headers.TryGetValue("X-CareHub-Document-Key", out StringValues provided)
                || string.IsNullOrEmpty(provided)
                || !string.Equals(provided[0], expectedKey, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }

            return await next(ic);
        });

        group.MapGet(
                "/lab-orders/{labOrderId:guid}/document-context",
                async Task<IResult> (Guid labOrderId, LabOrderService svc, CancellationToken ct) =>
                {
                    var ctx = await svc.GetDocumentContextAsync(labOrderId, ct);
                    return ctx is null ? Results.NotFound() : Results.Ok(ctx);
                })
            .AllowAnonymous();

        return app;
    }
}
