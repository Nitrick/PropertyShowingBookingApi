using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PropertyShowingBookingApi.Contracts;
using PropertyShowingBookingApi.Data;
using PropertyShowingBookingApi.Domain;
using PropertyShowingBookingApi.Services;
using Xunit;

namespace PropertyShowingBookingApi.Tests;

public sealed class BookingServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private BookingService _service = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        await _db.Database.EnsureCreatedAsync();
        _service = new BookingService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<Guid> CreateFutureSlot(int capacity = 5)
    {
        var result = await _service.CreateSlotAsync(
            new CreateSlotRequest(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(2), 30, capacity), default);
        return result.Id;
    }

    [Fact]
    public async Task Booking_ConsumesSeats()
    {
        var slotId = await CreateFutureSlot(5);
        var booking = await _service.CreateBookingAsync(slotId, new CreateBookingRequest("renter@example.com", 3), default);
        var slots = await _service.GetSlotsAsync(null, null, null, 1, 20, default);
        Assert.Equal("Confirmed", booking.Status);
        Assert.Equal(2, Assert.Single(slots.Items).SeatsRemaining);
    }

    [Fact]
    public async Task Booking_CannotExceedCapacity()
    {
        var slotId = await CreateFutureSlot(2);
        await _service.CreateBookingAsync(slotId, new CreateBookingRequest("one@example.com", 2), default);
        var error = await Assert.ThrowsAsync<ApiException>(() =>
            _service.CreateBookingAsync(slotId, new CreateBookingRequest("two@example.com", 1), default));
        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public async Task Renter_CannotHaveTwoActiveBookingsForSameSlot()
    {
        var slotId = await CreateFutureSlot(10);
        await _service.CreateBookingAsync(slotId, new CreateBookingRequest("same@example.com", 1), default);
        var error = await Assert.ThrowsAsync<ApiException>(() =>
            _service.CreateBookingAsync(slotId, new CreateBookingRequest("same@example.com", 1), default));
        Assert.Equal(409, error.StatusCode);

        var slots = await _service.GetSlotsAsync(null, null, null, 1, 20, default);
        Assert.Equal(9, Assert.Single(slots.Items).SeatsRemaining);
    }

    [Fact]
    public async Task Cancellation_FreesSeats_AndWritesHistory()
    {
        var slotId = await CreateFutureSlot(3);
        var booking = await _service.CreateBookingAsync(slotId, new CreateBookingRequest("renter@example.com", 2), default);
        await _service.TransitionAsync(booking.Id,
            new TransitionBookingRequest("Cancelled", "Plans changed", "renter"), default);
        var slots = await _service.GetSlotsAsync(null, null, null, 1, 20, default);
        var history = await _service.GetHistoryAsync(booking.Id, default);
        Assert.Equal(3, Assert.Single(slots.Items).SeatsRemaining);
        Assert.Contains(history, x => x.To == "Cancelled" && x.Actor == "renter");
    }


    [Fact]
    public async Task CancelledBooking_AllowsSameRenterToBookAgain()
    {
        var slotId = await CreateFutureSlot(5);
        var first = await _service.CreateBookingAsync(
            slotId, new CreateBookingRequest("repeat@example.com", 2), default);

        await _service.TransitionAsync(first.Id,
            new TransitionBookingRequest("Cancelled", "Plans changed", "renter"), default);

        var second = await _service.CreateBookingAsync(
            slotId, new CreateBookingRequest("repeat@example.com", 2), default);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("Confirmed", second.Status);

        var slots = await _service.GetSlotsAsync(null, null, null, 1, 20, default);
        Assert.Equal(3, Assert.Single(slots.Items).SeatsRemaining);
    }

    [Fact]
    public async Task OwnerCanMarkBookingAttended_AfterSlotStarts()
    {
        var slotId = await CreateFutureSlot();
        var booking = await _service.CreateBookingAsync(
            slotId, new CreateBookingRequest("attended@example.com", 2), default);
        await MoveSlotStartIntoPast(slotId);

        var result = await _service.TransitionAsync(booking.Id,
            new TransitionBookingRequest("Attended", "Renter arrived", "owner"), default);

        Assert.Equal("Attended", result.Status);
        var history = await _service.GetHistoryAsync(booking.Id, default);
        Assert.Contains(history, x => x.From == "Confirmed" && x.To == "Attended"
            && x.Actor == "owner" && x.Reason == "Renter arrived");
    }

    [Fact]
    public async Task OwnerCanMarkBookingNoShow_AfterSlotStarts()
    {
        var slotId = await CreateFutureSlot();
        var booking = await _service.CreateBookingAsync(
            slotId, new CreateBookingRequest("noshow@example.com", 1), default);
        await MoveSlotStartIntoPast(slotId);

        var result = await _service.TransitionAsync(booking.Id,
            new TransitionBookingRequest("NoShow", "Renter did not arrive", "owner"), default);

        Assert.Equal("NoShow", result.Status);
        var history = await _service.GetHistoryAsync(booking.Id, default);
        Assert.Contains(history, x => x.To == "NoShow" && x.Actor == "owner");
    }

    [Fact]
    public async Task NoShow_DoesNotReleaseReservedSeats()
    {
        var slotId = await CreateFutureSlot(5);
        var booking = await _service.CreateBookingAsync(
            slotId, new CreateBookingRequest("no-release@example.com", 2), default);
        await MoveSlotStartIntoPast(slotId);

        await _service.TransitionAsync(booking.Id,
            new TransitionBookingRequest("NoShow", "Did not attend", "owner"), default);

        var slots = await _service.GetSlotsAsync(null, null, null, 1, 20, default);
        Assert.Equal(3, Assert.Single(slots.Items).SeatsRemaining);
    }

    [Fact]
    public async Task OwnerCannotMarkBookingAttended_BeforeSlotStarts()
    {
        var slotId = await CreateFutureSlot();
        var booking = await _service.CreateBookingAsync(
            slotId, new CreateBookingRequest("future@example.com", 1), default);

        var error = await Assert.ThrowsAsync<ApiException>(() => _service.TransitionAsync(
            booking.Id, new TransitionBookingRequest("Attended", "Too early", "owner"), default));

        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public async Task RenterCannotMarkBookingNoShow()
    {
        var slotId = await CreateFutureSlot();
        var booking = await _service.CreateBookingAsync(
            slotId, new CreateBookingRequest("renter-noshow@example.com", 1), default);
        await MoveSlotStartIntoPast(slotId);

        var error = await Assert.ThrowsAsync<ApiException>(() => _service.TransitionAsync(
            booking.Id, new TransitionBookingRequest("NoShow", "Not allowed", "renter"), default));

        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public async Task FinalBookingState_CannotBeChangedAgain()
    {
        var slotId = await CreateFutureSlot();
        var booking = await _service.CreateBookingAsync(
            slotId, new CreateBookingRequest("final@example.com", 1), default);
        await _service.TransitionAsync(booking.Id,
            new TransitionBookingRequest("Cancelled", "Cancelled by renter", "renter"), default);

        var error = await Assert.ThrowsAsync<ApiException>(() => _service.TransitionAsync(
            booking.Id, new TransitionBookingRequest("Attended", "Try again", "owner"), default));

        Assert.Equal(409, error.StatusCode);
    }

    private async Task MoveSlotStartIntoPast(Guid slotId)
    {
        var slot = await _db.Slots.SingleAsync(x => x.Id == slotId);
        slot.StartsAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task InvalidSlotDuration_IsRejected()
    {
        var error = await Assert.ThrowsAsync<ApiException>(() => _service.CreateSlotAsync(
            new CreateSlotRequest(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), 10, 5), default));
        Assert.Equal(400, error.StatusCode);
    }
}
