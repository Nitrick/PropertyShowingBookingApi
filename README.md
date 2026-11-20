# Property Showing Booking API

A .NET 8 HTTP API for property showing slots and renter bookings.

See the source, tests, and docs in this repository. Follow the setup and test instructions below.

## Technology
- ASP.NET Core Minimal APIs (.NET 8)
- Entity Framework Core 8 with SQLite
- xUnit and ASP.NET Core integration testing

## Run
```bash
dotnet restore
dotnet build
dotnet run --project src/PropertyShowingBookingApi
```

Open `/swagger` on the local application URL to explore the API.

## Test
```bash
dotnet test
```

## Design and business rules
See `docs/ARCHITECTURE.md`, `docs/BUSINESS-RULES.md`, `docs/DATA-MODEL.md`, and `docs/API-CONTRACT.md`.

## AI use
See `docs/AI-PROMPTS.md` and the AI use section below in the project README history for the prompts, changes, and rejected suggestions used during development.

## Notes
The `actor` request field follows the take-home assignment contract; it is not authentication or authorization. A production implementation should derive actor identity and permissions from authenticated claims.

The project has been manually exercised through Swagger, including simultaneous booking requests that produced one successful booking and one capacity conflict. Run the automated test suite locally before relying on a build result.
