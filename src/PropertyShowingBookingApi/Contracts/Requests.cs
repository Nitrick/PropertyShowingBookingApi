namespace PropertyShowingBookingApi.Contracts;

public sealed record CreateSlotRequest(Guid PropertyId, DateTimeOffset StartsAt, int DurationMinutes, int Capacity);
public sealed record CreateBookingRequest(string RenterEmail, int PartySize);
public sealed record TransitionBookingRequest(string To, string Reason, string Actor);
