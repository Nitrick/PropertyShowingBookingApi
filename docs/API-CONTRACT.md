# API Contract

Base URL: `https://localhost:<port>` or the HTTP URL printed by `dotnet run`.

## POST /slots

Request fields: `propertyId` (GUID), `startsAt` (future ISO-8601 timestamp), `durationMinutes` (15–120), `capacity` (1–20).

Success: `200 OK` with the created slot representation.

## GET /slots

Query fields: optional `propertyId`, `from`, `to`; `page` defaults to 1; `pageSize` defaults to 20 and maximum is 100.

Success: `200 OK` with `items`, `page`, `pageSize`. Items are ordered by start time and include seats remaining.

## POST /slots/{id}/bookings

Request: `renterEmail`, `partySize` (1–6).

Success: `200 OK` with a Confirmed booking.

## POST /bookings/{id}/transitions

Request: `to`, `reason`, `actor`.

Success: `200 OK` with the updated booking.

## GET /bookings/{id}/history

Success: `200 OK` with chronological state-change entries.

## Status codes

| Code | Meaning |
|---|---|
| 200 | Request completed |
| 400 | Invalid request or input |
| 404 | Slot/booking not found |
| 409 | Capacity or business-rule conflict |
