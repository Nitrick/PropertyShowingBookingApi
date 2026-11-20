namespace PropertyShowingBookingApi.Domain;

public sealed class BookingHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }
    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Actor { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
