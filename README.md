# Property Showing Booking API

A .NET 8 HTTP API for publishing property showing slots and allowing renters to reserve seats.

## Business rules implemented

- Owners can create future showing slots with a duration of 15–120 minutes and capacity of 1–20.
- Renters can book 1–6 seats per booking.
- A slot cannot be overbooked.
- A renter can have only one active (`Confirmed`) booking per slot.
- `Confirmed -> Cancelled` is allowed for owner/renter only before the slot starts; seats are released.
- `Confirmed -> Attended` and `Confirmed -> NoShow` are owner-only and allowed only after the slot starts.
- `Cancelled`, `Attended` and `NoShow` are final states.
- Booking state history records timestamp, actor and reason.

## Technology decisions

- **.NET 8 / ASP.NET Core Minimal APIs:** small HTTP surface with clear endpoint mapping.
- **EF Core 8 + SQLite:** durable local persistence without requiring Docker or a separately installed database server.
- **Atomic capacity update:** booking uses a conditional SQL update (`BookedSeats + partySize <= Capacity`) inside a database transaction. This is the capacity gate; a prior read alone would be unsafe under concurrent requests.
- **Unique filtered index:** `(SlotId, RenterEmail)` is unique while `Status = 'Confirmed'`, providing a database-level guard against duplicate active bookings.
- **xUnit:** relational SQLite service tests cover capacity, duplicate active bookings, cancellation/rebooking, seat accounting, transition rules and history. An HTTP endpoint smoke test exercises `POST /slots` through the ASP.NET Core test host.

SQLite serializes write transactions. For a production multi-instance deployment, review database choice, retry policy, lock timeout, observability and load testing. A SQL Server implementation could use an equivalent conditional update and transaction.

## Prerequisites

- .NET SDK 8.0
- Windows PowerShell, Command Prompt, macOS Terminal or Linux shell

Check SDK:

```bash
dotnet --list-sdks
```

## Run the API

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project src/PropertyShowingBookingApi
```

The terminal prints the local HTTP/HTTPS addresses. Open `/swagger` on the HTTP/HTTPS address to explore the endpoints. The SQLite file `property-showings.db` is created in the API working directory on first run.

If HTTPS development certificates are not configured, use the HTTP URL printed by the application.

## Run automated tests

```bash
dotnet test
```

The test suite includes service-level tests using a relational SQLite in-memory database and an HTTP smoke test using `WebApplicationFactory`. The tests are source changes in this handover; run them locally before submission and record the actual output. A successful local run should report 13 passing tests (subject to any environment-specific restore/build issues).

## API endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/slots` | Create a showing slot |
| GET | `/slots?propertyId=&from=&to=&page=&pageSize=` | Filter and page slots |
| POST | `/slots/{id}/bookings` | Create a renter booking |
| POST | `/bookings/{id}/transitions` | Change booking state |
| GET | `/bookings/{id}/history` | Read state history |

### Create a slot

```http
POST /slots
Content-Type: application/json
```

```json
{
  "propertyId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "startsAt": "2030-05-20T10:00:00+05:30",
  "durationMinutes": 30,
  "capacity": 8
}
```

### List slots

```http
GET /slots?page=1&pageSize=20
```

Optional filters: `propertyId`, `from`, `to`. Results are ordered by `startsAt` ascending and include `seatsRemaining`. `pageSize` must be 1–100.

### Book a slot

```http
POST /slots/{slotId}/bookings
Content-Type: application/json
```

```json
{
  "renterEmail": "renter@example.com",
  "partySize": 2
}
```

### Transition a booking

```http
POST /bookings/{bookingId}/transitions
Content-Type: application/json
```

```json
{
  "to": "Cancelled",
  "reason": "Schedule changed",
  "actor": "renter"
}
```

Allowed targets: `Cancelled`, `Attended`, `NoShow`. Actor: `owner` or `renter`.

### Get booking history

```http
GET /bookings/{bookingId}/history
```

## Error behavior

- `400 Bad Request`: invalid input or unsupported target.
- `404 Not Found`: slot or booking does not exist.
- `409 Conflict`: insufficient capacity, duplicate active booking, invalid state transition or timing/permission violation.

## Manual verification status and sequence

Manual verification already supplied by the developer during implementation:

- Slot creation and slot listing succeeded through Swagger.
- Booking creation and cancellation succeeded.
- Booking history returned both the initial `Confirmed -> Confirmed` record and the later `Confirmed -> Cancelled` record.
- An attempted transition from a cancelled booking returned `409` as required.
- A simultaneous two-renter booking race for a capacity-5 slot (each requested 3 seats) produced one `200` and one `409`; the subsequent slot listing showed 2 seats remaining. This verifies the observed manual race scenario, not a load test across multiple application instances.
- An owner attempt to mark a future booking `NoShow` returned `409` as required.

Run the remaining checks in `docs/FINAL-VERIFICATION-CHECKLIST.md`, especially successful `Attended`/`NoShow` after start, duplicate active booking, cancellation/rebooking and final seat accounting. The source changes in this handover have not been built or tested in this execution environment; run `dotnet test` locally and record the actual result before submission.

## Project structure

```text
PropertyShowingBookingApi/
├── src/
│   └── PropertyShowingBookingApi/
│       ├── Contracts/
│       ├── Data/
│       ├── Domain/
│       ├── Endpoints/
│       ├── Services/
│       ├── Program.cs
│       └── appsettings.json
├── tests/
│   └── PropertyShowingBookingApi.Tests/
│       ├── ApiFactory.cs
│       ├── BookingServiceTests.cs
│       └── SlotEndpointTests.cs
├── docs/
│   ├── ARCHITECTURE.md
│   ├── API-CONTRACT.md
│   ├── INTEGRATION.md
│   ├── BUSINESS-RULES.md
│   ├── DATA-MODEL.md
│   ├── TEST-PLAN.md
│   ├── IMPLEMENTATION-LOG.md
│   ├── AI-PROMPTS.md
│   ├── TROUBLESHOOTING.md
│   ├── FINAL-VERIFICATION-CHECKLIST.md
│   └── INTERVIEW-GUIDE.md
└── README.md
```

## Design trade-offs and next steps

This is a focused take-home implementation, not a production-ready property platform. With more time:

- Add authentication/authorization and derive the actor from the authenticated principal rather than trusting the request body.
- Add structured Problem Details responses and centralized exception handling.
- Add migrations instead of `EnsureCreated`.
- Add integration tests using `WebApplicationFactory` and a relational test database.
- Add multi-process/multi-instance concurrency load tests against the production database provider.
- Add cancellation/transition idempotency policy, audit correlation IDs and structured logs.
- Consider SQL Server for multi-instance production workloads and test its locking/isolation behavior.
- Add property ownership validation, email verification and notification delivery.

## AI use

AI assistance was used to help plan the API structure, identify edge cases, draft implementation code, propose tests and prepare documentation. The developer reviewed and adjusted the generated code. Examples of rejected/corrected AI output include an initial PowerShell `curl.exe` concurrency command that sent malformed JSON and a proposed transition refactor that would have removed the conditional database status update. The corrected test uses `Invoke-RestMethod`; the final transition implementation retains the atomic `Confirmed` status predicate. See `docs/AI-PROMPTS.md` for the prompt playbook and review notes.

The expanded tests in this handover have not been executed in the current environment. Run the restore/build/test commands and record the actual results before submission. In particular, repeat concurrency verification locally. The actor supplied in the request body is only a take-home contract input, not production authorization; replace it with authenticated identity and server-side ownership checks in a real deployment.

See `docs/INTEGRATION.md` for client integration steps and `docs/FINAL-VERIFICATION-CHECKLIST.md` for the final manual test sequence. See `docs/IMPLEMENTATION-LOG.md` for implementation decisions and observed results.
