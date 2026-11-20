# Data Model

## ShowingSlot

| Field | Type | Purpose |
|---|---|---|
| Id | GUID | Slot identifier |
| PropertyId | GUID | Property identifier |
| StartsAt | DateTimeOffset | Start time |
| DurationMinutes | int | Slot duration |
| Capacity | int | Maximum seats |
| BookedSeats | int | Currently reserved seats |
| CreatedAt | DateTimeOffset | Creation timestamp |

`SeatsRemaining` is calculated as `Capacity - BookedSeats`.

## Booking

| Field | Type | Purpose |
|---|---|---|
| Id | GUID | Booking identifier |
| SlotId | GUID | Parent slot |
| RenterEmail | string | Normalized renter email |
| PartySize | int | Seats consumed |
| Status | enum | Confirmed/Cancelled/Attended/NoShow |
| CreatedAt | DateTimeOffset | Creation timestamp |

## BookingHistory

| Field | Type | Purpose |
|---|---|---|
| Id | GUID | History identifier |
| BookingId | GUID | Parent booking |
| FromStatus | enum | Previous state |
| ToStatus | enum | New state |
| ChangedAt | DateTimeOffset | Change timestamp |
| Actor | string | owner/renter |
| Reason | string? | Optional explanation |

## Constraints and indexes

- Slot capacity check: 1–20.
- Booked seat check: 0 through capacity.
- Slot query index: `(PropertyId, StartsAt)`.
- Unique filtered active booking index: `(SlotId, RenterEmail)` where status is Confirmed.
