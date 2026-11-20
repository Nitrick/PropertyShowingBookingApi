# Architecture

## Overview

The solution uses a deliberately small layered design:

- **Endpoints:** HTTP routing, request binding and response mapping.
- **Contracts:** request and response DTOs; API shape is separated from persistence entities.
- **Services:** use-case orchestration, validation and business rules.
- **Domain:** core entities and booking status enum.
- **Data:** EF Core DbContext, relational mappings and database constraints.

## Request flow

```text
HTTP Request
    |
    v
Minimal API Endpoint
    |
    v
BookingService (validation + use case)
    |
    v
AppDbContext / EF Core
    |
    v
SQLite
```

## Why not full Clean Architecture / MediatR?

The assignment has a strict three-hour budget and a small number of use cases. Separate projects, MediatR handlers and extensive abstractions would add ceremony without materially improving this small API. The code keeps boundaries clear and can be split into projects if the domain grows.

## Concurrency approach

Booking uses a transaction and one conditional database update:

```sql
UPDATE Slots
SET BookedSeats = BookedSeats + @partySize
WHERE Id = @slotId
  AND BookedSeats + @partySize <= Capacity;
```

A successful affected-row count of 1 reserves the seats. A count of 0 means the slot does not have enough capacity (or no matching slot). The service loads the slot first to distinguish not-found from capacity conflict. The database check constraint protects `0 <= BookedSeats <= Capacity`.

A filtered unique index prevents more than one Confirmed booking for the same slot/email. Cancellation first applies a conditional `Confirmed`-only status update, then releases seats in the same transaction and writes history. The affected-row checks prevent a repeated transition from releasing seats twice; if seat release fails, transaction disposal rolls the status update back.

## Important limitation

The `actor` value in this take-home API is supplied by the caller because the assignment defines it in the request body. It is not authentication. A production system must obtain the actor from the authenticated identity and enforce owner/renter authorization server-side.
