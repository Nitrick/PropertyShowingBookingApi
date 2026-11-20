# Implementation Log

This file is a living engineering journal. Update it with the actual commands, test output, defects and fixes observed on your machine. Do not claim a test passed until you have run it.

## Stage 1 — Understand the assignment

- Extracted actors, endpoints, state transitions, validation boundaries and persistence expectations.
- Identified concurrency and duplicate active bookings as the highest-risk requirements.
- Selected .NET 8, ASP.NET Core Minimal APIs, EF Core and SQLite.

## Stage 2 — Create solution structure

- Created API and xUnit test projects.
- Separated Domain, Data, Contracts, Services and Endpoints.
- Added solution-level README and focused technical documents.

## Stage 3 — Implement persistence

- Added ShowingSlot, Booking and BookingHistory entities.
- Added relational constraints and indexes.
- Selected SQLite so local evaluation does not require Docker.

## Stage 4 — Implement use cases

- Slot creation and paginated/filterable listing.
- Booking with conditional seat reservation in a transaction.
- Transition rules and seat release on cancellation.
- Booking history retrieval.

## Stage 5 — Add automated tests

- Added service-level tests using a relational SQLite in-memory connection.
- Run `dotnet test` locally and append actual results below.

## Stage 6 — Manual verification and interview preparation

- Follow README manual verification sequence.
- Record any bug, root cause, fix and regression test here.
- Be ready to explain the atomic update, filtered unique index, transaction boundary and authorization limitation.

## Local execution record

| Date | Command | Result | Notes |
|---|---|---|---|
| To be completed | `dotnet build` | Pending | Run locally |
| To be completed | `dotnet test` | Pending | Run locally |
| To be completed | Swagger manual test | Pending | Record outcomes |


## Final handover update — 2026-11-20

- Reviewed the uploaded project archive and preserved the existing Minimal API / EF Core / SQLite structure.
- Hardened `TransitionAsync` with an atomic `UPDATE Bookings ... WHERE Status = 'Confirmed'` guard before cancellation seat release. Status change, seat release and history insertion remain in one serializable transaction.
- Added service tests for same-renter rebooking after cancellation, successful owner Attended/NoShow after slot start, timing/actor restrictions, final-state protection, NoShow seat accounting and duplicate-booking seat accounting.
- Added a `WebApplicationFactory` HTTP smoke test for `POST /slots` using a shared in-memory SQLite connection.
- Updated README, architecture and test plan. Added a manual final-verification checklist.
- Manual results supplied by the developer: cancellation history contained both expected records; a simultaneous 3-seat + 3-seat race against capacity 5 yielded one `200` and one `409`, with 2 seats remaining; invalid future attendance/no-show attempts were rejected with `409`.
- The first PowerShell concurrency command using `curl.exe` sent malformed JSON (HTTP 400). It was replaced with `Invoke-RestMethod` in background jobs; the corrected race completed with the expected `200`/`409` outcomes.
- A tracked-entity-only transition suggestion was not used because it would weaken the database-level conditional transition guard. The final implementation keeps the conditional SQL state update.
- **Not verified in this handover environment:** `dotnet restore`, `dotnet build`, and the expanded `dotnet test` suite. The execution container did not have the .NET SDK installed. The developer must run the commands locally and report the actual output before final submission.
