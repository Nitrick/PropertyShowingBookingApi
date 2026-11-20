using PropertyShowingBookingApi.Contracts;
using PropertyShowingBookingApi.Services;

namespace PropertyShowingBookingApi.Endpoints;

public static class SlotEndpoints
{
    public static IEndpointRouteBuilder MapSlotEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/slots").WithTags("Slots");
        group.MapPost("/", async (CreateSlotRequest request, BookingService service, CancellationToken ct) =>
            await Execute(() => service.CreateSlotAsync(request, ct)));
        group.MapGet("/", async (Guid? propertyId, DateTimeOffset? from, DateTimeOffset? to,
            int page = 1, int pageSize = 20, BookingService service = null!, CancellationToken ct = default) =>
            await Execute(async () =>
            {
                var result = await service.GetSlotsAsync(propertyId, from, to, page, pageSize, ct);
                return Results.Ok(new { result.Items, result.Page, result.PageSize });
            }));
        return app;
    }

    internal static async Task<IResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Results.Ok(await action()); }
        catch (ApiException ex) { return Results.Json(new { error = ex.Message }, statusCode: ex.StatusCode); }
    }
}
