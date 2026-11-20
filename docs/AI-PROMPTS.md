# AI Prompt Playbook — Property Showing Booking API

These are reusable prompts aligned with the actual development stages. Use them with your coding assistant. Review generated code, run it, and keep a record of accepted/rejected suggestions. Do not paste secrets or real renter data into prompts.

## Prompt 1 — Requirement analysis

```text
Act as a senior .NET 8 backend engineer and technical reviewer.
Analyze the Property Showing Booking API take-home requirements I provide.
Do not write code yet.
Extract:
1. Actors and use cases.
2. Exact endpoints and request/response contracts.
3. Validation rules and boundary values.
4. Booking state machine and allowed/forbidden transitions.
5. Data entities, relationships, constraints and indexes.
6. Concurrency risks, especially two renters booking the last seats.
7. A minimal implementation plan that fits a three-hour take-home assignment.
Separate explicit requirements from assumptions. Flag ambiguity instead of inventing requirements.
```

## Prompt 2 — Architecture and project structure

```text
Design a small, interview-ready ASP.NET Core .NET 8 Web API for the Property Showing Booking assignment.
Use C#, EF Core and SQLite. Prefer a simple layered structure with Domain, Data, Contracts,
Services and Endpoints. Avoid unnecessary abstractions and avoid overengineering.
Provide the folder tree, responsibility of each folder, dependency direction, package references,
and explain why each decision fits a three-hour exercise. Do not implement code yet.
```

## Prompt 3 — Domain model and database

```text
Implement EF Core entities and AppDbContext for ShowingSlot, Booking and BookingHistory.
Requirements: slots have capacity and booked seats; bookings have Confirmed, Cancelled, Attended,
NoShow states; history stores from/to status, timestamp, actor and reason.
Add relational constraints for capacity and booked seats, useful indexes, and a filtered unique
index allowing only one Confirmed booking per slot and normalized renter email.
Use SQLite-compatible EF Core 8 mappings. Explain migration/schema implications and provide code.
```

## Prompt 4 — Slot endpoints

```text
Implement POST /slots and GET /slots for the .NET 8 API.
POST validates non-empty propertyId, future startsAt, durationMinutes 15..120 and capacity 1..20.
GET supports propertyId/from/to/page/pageSize, orders by startsAt ascending, returns seatsRemaining,
defaults page=1 and pageSize=20, and rejects pageSize above 100.
Use DTOs, async EF Core, cancellation tokens and clear 400 errors. Include sample HTTP requests.
```

## Prompt 5 — Concurrency-safe booking

```text
Act as a database concurrency specialist.
Implement POST /slots/{id}/bookings using EF Core 8 and SQLite.
Validate renterEmail and partySize 1..6. A booking consumes partySize seats and starts Confirmed.
A renter may have only one active booking per slot. Never allow BookedSeats > Capacity under
simultaneous requests.
Do not rely on read-then-check application logic. Use a transaction and an atomic conditional
database update (BookedSeats + requested seats <= Capacity), check affected rows, and a database
unique filtered index for active bookings. Explain SQLite behavior, transaction boundaries,
rollback behavior, and what should change for SQL Server or a multi-instance deployment.
Return 404 for missing slot, 400 for invalid input and 409 for business conflicts.
```

## Prompt 6 — Booking transitions

```text
Implement POST /bookings/{id}/transitions.
Only Confirmed can transition. Confirmed -> Cancelled is allowed for owner or renter only before
the slot starts and must release seats exactly once. Confirmed -> Attended or NoShow is owner-only
and only after the slot starts. Cancelled, Attended and NoShow are final.
Use a transaction and conditional state update to avoid duplicate transitions. Add a history row
with timestamp, actor and reason in the same transaction. Return 404 for missing booking,
400 for malformed target/actor and 409 for forbidden transitions. Explain each branch.
```

## Prompt 7 — Booking history

```text
Implement GET /bookings/{id}/history.
Return every state-change record in chronological order with from status, to status, timestamp,
actor and reason. Return 404 when the booking does not exist. Use no-tracking EF Core queries
and provide response examples.
```

## Prompt 8 — Automated tests

```text
Create xUnit tests for the Property Showing Booking API business rules using a relational
SQLite in-memory connection, not EF Core InMemory.
Cover successful booking and seat count, capacity conflict, duplicate active booking,
cancellation releasing seats and writing history, invalid slot validation, final-state protection,
owner-only attendance/no-show and before/after-start timing.
For concurrency, explain how to add a separate multi-connection integration/load test.
Ensure each test is isolated and deterministic; do not claim tests pass without execution.
```

## Prompt 9 — Code review and defect analysis

```text
Review the supplied .NET 8 Property Showing Booking API as a strict senior engineer.
Check every assignment requirement against the code.
Prioritize correctness, concurrency, transactions, seat accounting, duplicate active bookings,
state transitions, validation, HTTP status codes, EF Core/SQLite compatibility and test coverage.
For every finding provide severity, exact file/area, reproduction steps, root cause and minimal fix.
Do not rewrite unrelated code. Identify assumptions and production security gaps separately.
```

## Prompt 10 — README and handover documentation

```text
Create developer-facing Markdown documentation for this repository:
README, architecture, API contract, business rules/state machine, data model, test plan,
implementation log, troubleshooting and interview guide.
Include exact prerequisites, restore/build/run/test commands, Swagger usage, sample JSON,
manual verification steps, design trade-offs, limitations, future improvements and AI-use disclosure.
Do not invent test results, ports, deployment URLs or completed features. Mark anything the developer
must verify locally as Pending.
```

## Prompt 11 — Interview explanation

```text
Act as a senior backend interviewer reviewing my Property Showing Booking API.
Ask me to explain the architecture, why SQLite, how the atomic seat update prevents overbooking,
why a filtered unique index is needed, transaction boundaries, cancellation seat release,
state transitions, testing strategy and production authorization gaps.
Ask one question at a time. After my answer, evaluate technical correctness and give a natural
30–40 second senior-level answer with a concrete example from this application.
```

## How to use the prompts responsibly

1. Give the assistant the assignment text and Prompt 1.
2. Review the analysis before requesting code.
3. Use one implementation prompt at a time; ask it to modify only the relevant files.
4. Build and test after each stage.
5. Paste actual compiler/test errors into a debugging prompt with the relevant code and full error.
6. Record what the assistant got wrong and what you changed in `IMPLEMENTATION-LOG.md`.
7. Never say a generated solution is production-ready without security, concurrency and integration testing.


## AI review record for this implementation

The prompts above are a reusable prompt playbook. The following are concrete review decisions from this implementation and should be retained as part of the AI-use disclosure:

- An initial generated PowerShell concurrency command using `curl.exe` produced malformed JSON. The API returned HTTP 400 with a JSON parser error. The request construction was replaced with `Invoke-RestMethod` in separate PowerShell background jobs; the corrected manual race produced one `200` and one `409`.
- A proposed transition refactor that relied only on changing the tracked EF Core entity was not accepted because it removed the database-level `Status = 'Confirmed'` conditional update. The final implementation retains an atomic conditional status update so a repeated transition cannot release seats twice.
- SQLite DateTimeOffset comparison required storing timestamps as UTC ticks through an EF Core value converter; the converter is present in `AppDbContext`.
- The actor remains request-supplied to match the take-home contract. This is not secure authorization and must be replaced with authenticated identity in a production system.

AI-generated code and documentation were reviewed against the assignment and manual observations. The expanded tests still require local execution; do not represent them as passed until `dotnet test` succeeds.
