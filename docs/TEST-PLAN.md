# Test Plan

## Automated tests in this handover

The test project uses a relational SQLite in-memory connection for service tests. It also contains an HTTP endpoint smoke test using `WebApplicationFactory` and a shared in-memory SQLite connection.

1. Successful booking consumes the requested number of seats.
2. Booking cannot exceed remaining capacity.
3. A renter cannot hold two active bookings for the same slot; a rejected duplicate does not consume extra seats.
4. Cancellation releases seats and writes a history entry.
5. A cancelled booking can be booked again by the same renter.
6. Owner can mark a booking Attended after the slot starts.
7. Owner can mark a booking NoShow after the slot starts.
8. Attended is rejected before the slot starts.
9. Renter cannot mark a booking NoShow.
10. NoShow does not release reserved seats.
11. Final booking states cannot transition again.
12. Invalid slot duration is rejected.
13. `POST /slots` returns a successful HTTP response with the created slot and initial availability.

Run:

```powershell
dotnet test .\PropertyShowingBookingApi.sln --logger "console;verbosity=normal"
```

Expected test count for this version: 13. This is an expected count from the checked-in test cases, not a claim that the suite has already run in the handover environment.

## Manual API verification

| Scenario | Expected |
|---|---|
| Create future slot with valid duration/capacity | 200 |
| Create slot with past start | 400 |
| Create slot with duration 14 or 121 | 400 |
| Create slot with capacity 0 or 21 | 400 |
| List slots with pageSize 101 | 400 |
| Book party size 0 or 7 | 400 |
| Book unknown slot | 404 |
| Book more than remaining seats | 409 |
| Duplicate active renter/slot | 409 |
| Cancel future booking as renter or owner | 200 |
| Cancel after slot starts | 409 |
| Renter marks Attended or NoShow | 409 |
| Owner marks future booking Attended/NoShow | 409 |
| Owner marks booking Attended/NoShow after start | 200 |
| Transition a final state | 409 |
| Read unknown booking history | 404 |
| Cancellation followed by same-renter rebooking | New booking 200; seat count remains within capacity |

## Concurrency verification

The developer manually ran two simultaneous HTTP booking requests against a capacity-5 slot. Each requested 3 seats. The observed result was one `200`, one `409`, and a subsequent `GET /slots` reported 2 seats remaining. This is a successful manual scenario, not a substitute for repeated stress testing.

For further verification, use separate HTTP requests/processes and a real relational database. Assert that successful reservations never sum to more than capacity, and inspect final `BookedSeats`. SQLite serializes writers; SQL Server or another production provider must be tested with its actual isolation/locking behavior. EF Core InMemory is not suitable for validating relational locking behavior.

## Test execution record

- Earlier baseline: developer reported 5/5 tests passing before this final test expansion.
- This handover adds more tests and a WebApplicationFactory endpoint test. The current environment did not have the .NET SDK installed, so the expanded suite has **not** been executed here. Run it locally before treating the handover as submission-ready.
