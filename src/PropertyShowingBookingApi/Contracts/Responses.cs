using PropertyShowingBookingApi.Domain;

namespace PropertyShowingBookingApi.Contracts;

public sealed record SlotResponse(
    Guid Id, Guid PropertyId, DateTimeOffset StartsAt, int DurationMinutes,
    int Capacity, int SeatsRemaining);

public sealed record BookingResponse(
    Guid Id, Guid SlotId, string RenterEmail, int PartySize, string Status, DateTimeOffset CreatedAt);

public sealed record BookingHistoryResponse(
    string From, string To, DateTimeOffset ChangedAt, string Actor, string? Reason);
