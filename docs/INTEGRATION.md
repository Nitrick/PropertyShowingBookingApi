# Integration Guide

This guide explains how another client (Postman, Swagger UI, frontend application or another backend) integrates with the Property Showing Booking API.

## 1. Start the API

From the repository root:

```bash
dotnet restore
dotnet run --project src/PropertyShowingBookingApi --urls http://localhost:5088
```

Base URL: `http://localhost:5088`

Swagger UI: `http://localhost:5088/swagger`

## 2. Recommended integration sequence

### A. Create a showing slot

`POST /slots`

```json
{
  "propertyId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "startsAt": "2030-05-20T10:00:00+05:30",
  "durationMinutes": 30,
  "capacity": 8
}
```

Save the returned `id` as `slotId`.

### B. Retrieve available slots

`GET /slots?propertyId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&page=1&pageSize=20`

A client should display `seatsRemaining` and should not assume availability from a previously cached response. The booking endpoint is authoritative because availability can change between listing and booking.

### C. Book seats

`POST /slots/{slotId}/bookings`

```json
{
  "renterEmail": "renter@example.com",
  "partySize": 2
}
```

Save the returned booking `id` as `bookingId`.

### D. Cancel or record attendance

`POST /bookings/{bookingId}/transitions`

Cancellation before the slot starts:

```json
{
  "to": "Cancelled",
  "reason": "Schedule changed",
  "actor": "renter"
}
```

Attendance after the slot starts:

```json
{
  "to": "Attended",
  "reason": "Renter attended the showing",
  "actor": "owner"
}
```

No-show after the slot starts:

```json
{
  "to": "NoShow",
  "reason": "Renter did not arrive",
  "actor": "owner"
}
```

### E. Retrieve audit history

`GET /bookings/{bookingId}/history`

The response contains state, timestamp, actor and reason for each recorded entry.

## 3. Client integration guidance

- Treat `409 Conflict` as a business outcome (capacity changed, duplicate active booking, or invalid transition), not as a transient success.
- Refresh the slot list after a successful booking or cancellation.
- Do not rely on client-side validation for capacity or state transitions.
- Send timestamps as ISO-8601 values with an explicit UTC offset.
- Normalize renter email consistently; the API trims and lowercases it.
- Do not expose an owner-only action to renters in the UI. The API must still enforce authorization server-side in a production version.

## 4. Authentication and production boundary

This assignment's request body includes `actor` because the take-home contract specifies it. This is only a demonstration input and is not a secure identity mechanism. Before production integration:
1. Add authentication (for example, JWT bearer authentication).
2. Resolve the actor from the authenticated principal.
3. Verify that the owner owns the property/slot.
4. Verify that the renter is the booking owner before allowing renter cancellation.
5. Remove trust in caller-supplied `actor`.
