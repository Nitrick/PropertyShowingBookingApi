namespace PropertyShowingBookingApi.Domain;

public sealed class ShowingSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    // Maintained atomically in the same transaction as booking creation/cancellation.
    public int BookedSeats { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int SeatsRemaining => Capacity - BookedSeats;
}
