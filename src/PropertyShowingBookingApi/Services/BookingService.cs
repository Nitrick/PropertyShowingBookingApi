using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PropertyShowingBookingApi.Contracts;
using PropertyShowingBookingApi.Data;
using PropertyShowingBookingApi.Domain;

namespace PropertyShowingBookingApi.Services;

public sealed class BookingService(AppDbContext db)
{
    public async Task<SlotResponse> CreateSlotAsync(CreateSlotRequest request, CancellationToken ct)
    {
        if (request.PropertyId == Guid.Empty) throw new ApiException(400, "propertyId is required.");
        if (request.StartsAt <= DateTimeOffset.UtcNow) throw new ApiException(400, "startsAt must be in the future.");
        if (request.DurationMinutes is < 15 or > 120) throw new ApiException(400, "durationMinutes must be between 15 and 120.");
        if (request.Capacity is < 1 or > 20) throw new ApiException(400, "capacity must be between 1 and 20.");

        var slot = new ShowingSlot
        {
            PropertyId = request.PropertyId,
            StartsAt = request.StartsAt,
            DurationMinutes = request.DurationMinutes,
            Capacity = request.Capacity,
            BookedSeats = 0
        };
        db.Slots.Add(slot);
        await db.SaveChangesAsync(ct);
        return ToResponse(slot);
    }

    public async Task<(IReadOnlyList<SlotResponse> Items, int Page, int PageSize)> GetSlotsAsync(
        Guid? propertyId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) throw new ApiException(400, "page must be greater than or equal to 1.");
        if (pageSize is < 1 or > 100) throw new ApiException(400, "pageSize must be between 1 and 100.");
        if (from.HasValue && to.HasValue && from > to) throw new ApiException(400, "from must be earlier than or equal to to.");

        var query = db.Slots.AsNoTracking().AsQueryable();
        if (propertyId.HasValue) query = query.Where(x => x.PropertyId == propertyId.Value);
        if (from.HasValue) query = query.Where(x => x.StartsAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.StartsAt <= to.Value);

        var items = await query.OrderBy(x => x.StartsAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (items.Select(ToResponse).ToList(), page, pageSize);
    }

    public async Task<BookingResponse> CreateBookingAsync(Guid slotId, CreateBookingRequest request, CancellationToken ct)
    {
        var email = request.RenterEmail?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || email.Length > 320)
            throw new ApiException(400, "A valid renterEmail is required.");
        if (request.PartySize is < 1 or > 6)
            throw new ApiException(400, "partySize must be between 1 and 6.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var slot = await db.Slots.SingleOrDefaultAsync(x => x.Id == slotId, ct);
        if (slot is null) throw new ApiException(404, "Showing slot was not found.");
        if (slot.StartsAt <= DateTimeOffset.UtcNow) throw new ApiException(409, "Bookings are not allowed after a slot has started.");

        // The conditional update is the capacity gate. SQLite serializes writers; only an update
        // that keeps BookedSeats <= Capacity can succeed.
        var affected = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Slots SET BookedSeats = BookedSeats + {request.PartySize} WHERE Id = {slotId} AND BookedSeats + {request.PartySize} <= Capacity", ct);
        if (affected != 1) throw new ApiException(409, "Not enough seats remaining for this booking.");

        var duplicate = await db.Bookings.AnyAsync(x =>
            x.SlotId == slotId && x.RenterEmail == email && x.Status == BookingStatus.Confirmed, ct);
        if (duplicate) throw new ApiException(409, "This renter already has an active booking for this slot.");

        var booking = new Booking { SlotId = slotId, RenterEmail = email, PartySize = request.PartySize };
        booking.History.Add(new BookingHistory
        {
            FromStatus = BookingStatus.Confirmed,
            ToStatus = BookingStatus.Confirmed,
            Actor = "renter",
            Reason = "Booking created"
        });
        db.Bookings.Add(booking);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqliteException sqlite
            && sqlite.SqliteExtendedErrorCode == 2067)
        {
            // The filtered unique index is the final guard if two requests for the
            // same renter/slot pass the earlier application-level duplicate check.
            await tx.RollbackAsync(ct);
            throw new ApiException(409, "An active booking already exists for this renter and slot.");
        }
        return ToResponse(booking);
    }

    public async Task<BookingResponse> TransitionAsync(
        Guid bookingId,
        TransitionBookingRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<BookingStatus>(request.To, ignoreCase: true, out var target))
            throw new ApiException(400, "to must be Cancelled, Attended or NoShow.");

        var actor = request.Actor?.Trim().ToLowerInvariant();
        if (actor is not ("owner" or "renter"))
            throw new ApiException(400, "actor must be owner or renter.");

        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);

        // This operation uses guarded SQL updates below. Load the booking without
        // tracking so EF Core does not later try to overwrite the status using a
        // stale tracked snapshot after the SQL state transition has already run.
        var booking = await db.Bookings
            .AsNoTracking()
            .Include(x => x.Slot)
            .SingleOrDefaultAsync(x => x.Id == bookingId, ct);

        if (booking is null)
            throw new ApiException(404, "Booking was not found.");

        if (booking.Status != BookingStatus.Confirmed)
            throw new ApiException(
                409,
                "Only Confirmed bookings can transition. Final states cannot be changed.");

        var now = DateTimeOffset.UtcNow;
        if (target == BookingStatus.Cancelled)
        {
            if (booking.Slot!.StartsAt <= now)
                throw new ApiException(
                    409,
                    "A booking can only be cancelled before the slot starts.");
        }
        else if (target is BookingStatus.Attended or BookingStatus.NoShow)
        {
            if (actor != "owner")
                throw new ApiException(
                    409,
                    "Only the owner can mark a booking as Attended or NoShow.");

            if (booking.Slot!.StartsAt > now)
                throw new ApiException(
                    409,
                    "Attended or NoShow can only be recorded after the slot starts.");
        }
        else
        {
            throw new ApiException(400, "Unsupported transition.");
        }

        // The status predicate is an atomic state-transition guard. It prevents two
        // concurrent requests from both applying a transition to the same booking.
        var transitioned = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Bookings SET Status = {target.ToString()} WHERE Id = {bookingId} AND Status = 'Confirmed'",
            ct);

        if (transitioned != 1)
            throw new ApiException(409, "Booking has already been transitioned.");

        if (target == BookingStatus.Cancelled)
        {
            // Seat release and state transition share this transaction. If the seat
            // counter is inconsistent, throwing rolls the status update back as well.
            var released = await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Slots SET BookedSeats = BookedSeats - {booking.PartySize} WHERE Id = {booking.SlotId} AND BookedSeats >= {booking.PartySize}",
                ct);

            if (released != 1)
                throw new ApiException(
                    409,
                    "Unable to release seats. The slot's booked-seat count is inconsistent.");
        }

        // Persist only the audit row through EF. The booking status and (for
        // cancellation) slot seat counter have already been changed by guarded SQL
        // statements in this transaction. Do not attach/update the booking entity.
        db.BookingHistories.Add(new BookingHistory
        {
            BookingId = bookingId,
            FromStatus = BookingStatus.Confirmed,
            ToStatus = target,
            ChangedAt = now,
            Actor = actor!,
            Reason = string.IsNullOrWhiteSpace(request.Reason)
                ? null
                : request.Reason.Trim()
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new BookingResponse(
            booking.Id,
            booking.SlotId,
            booking.RenterEmail,
            booking.PartySize,
            target.ToString(),
            booking.CreatedAt);
    }

    public async Task<IReadOnlyList<BookingHistoryResponse>> GetHistoryAsync(Guid bookingId, CancellationToken ct)
    {
        if (!await db.Bookings.AnyAsync(x => x.Id == bookingId, ct))
            throw new ApiException(404, "Booking was not found.");
        return await db.BookingHistories.AsNoTracking().Where(x => x.BookingId == bookingId)
            .OrderBy(x => x.ChangedAt)
            .Select(x => new BookingHistoryResponse(x.FromStatus.ToString(), x.ToStatus.ToString(),
                x.ChangedAt, x.Actor, x.Reason)).ToListAsync(ct);
    }

    private static SlotResponse ToResponse(ShowingSlot x) =>
        new(x.Id, x.PropertyId, x.StartsAt, x.DurationMinutes, x.Capacity, x.Capacity - x.BookedSeats);

    private static BookingResponse ToResponse(Booking x) =>
        new(x.Id, x.SlotId, x.RenterEmail, x.PartySize, x.Status.ToString(), x.CreatedAt);
}

public sealed class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
