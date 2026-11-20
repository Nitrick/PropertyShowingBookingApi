# Business Rules and Scenarios

## Slot creation

- `propertyId` must not be an empty GUID.
- `startsAt` must be in the future.
- Duration must be 15–120 minutes inclusive.
- Capacity must be 1–20 inclusive.
- New slots begin with zero booked seats.

## Booking

- Party size must be 1–6 inclusive.
- Renter email is trimmed and normalized to lowercase.
- Slot must exist and must not have started.
- Booking begins as `Confirmed`.
- Reserved seats equal party size.
- Total booked seats must never exceed capacity.
- A renter can have only one Confirmed booking for a slot.
- A later booking after cancellation is permitted because Cancelled is not active.

## State machine

```text
                 +------------+
                 | Confirmed  |
                 +------------+
                   |    |    |
        before     |    |    | after start
        start      |    |    +------> NoShow (owner)
                   |    +-----------> Attended (owner)
                   +---------------> Cancelled (owner/renter)

Cancelled, Attended and NoShow are terminal states.
```

## Transition matrix

| Current | Target | Actor | Timing | Result |
|---|---|---|---|---|
| Confirmed | Cancelled | renter | Before start | Allowed; release seats |
| Confirmed | Cancelled | owner | Before start | Allowed; release seats |
| Confirmed | Attended | owner | At/after start | Allowed |
| Confirmed | NoShow | owner | At/after start | Allowed |
| Confirmed | Attended | renter | Any | Rejected |
| Confirmed | NoShow | renter | Any | Rejected |
| Confirmed | Cancelled | either | At/after start | Rejected |
| Final state | Any target | Any | Any | Rejected |

## Conflict examples

- Party size exceeds remaining seats: `409`.
- Same renter attempts another Confirmed booking on same slot: `409`.
- Attempt to cancel after the slot starts: `409`.
- Attempt to mark Attended before start: `409`.
- Attempt to transition a terminal booking: `409`.
