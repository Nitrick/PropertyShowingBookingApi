using PropertyShowingBookingApi.Contracts;
using PropertyShowingBookingApi.Services;

namespace PropertyShowingBookingApi.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/slots/{id:guid}/bookings", async (Guid id, CreateBookingRequest request, BookingService service, CancellationToken ct) =>
            await SlotEndpoints.Execute(() => service.CreateBookingAsync(id, request, ct)))
            .WithTags("Bookings");

        app.MapPost("/bookings/{id:guid}/transitions", async (Guid id, TransitionBookingRequest request, BookingService service, CancellationToken ct) =>
            await SlotEndpoints.Execute(() => service.TransitionAsync(id, request, ct)))
            .WithTags("Bookings");

        app.MapGet("/bookings/{id:guid}/history", async (Guid id, BookingService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetHistoryAsync(id, ct)); }
            catch (ApiException ex) { return Results.Json(new { error = ex.Message }, statusCode: ex.StatusCode); }
        }).WithTags("Bookings");

        return app;
    }
}
