namespace PropertyShowingBookingApi.Domain;

public sealed class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SlotId { get; set; }
    public ShowingSlot? Slot { get; set; }
    public string RenterEmail { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<BookingHistory> History { get; set; } = new List<BookingHistory>();
}
