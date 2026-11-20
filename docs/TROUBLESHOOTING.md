# Troubleshooting

## `dotnet` is not recognized

Install the .NET 8 SDK, open a new terminal and run `dotnet --list-sdks`.

## Package restore fails

Check network access and NuGet sources:

```bash
dotnet nuget list source
dotnet restore
```

## Port already in use

Run with a different local URL:

```bash
dotnet run --project src/PropertyShowingBookingApi --urls http://localhost:5088
```

## Swagger does not open

Use the exact URL printed by `dotnet run` and append `/swagger`. Swagger is enabled in Development. Check `ASPNETCORE_ENVIRONMENT`.

## SQLite database file

The app creates `property-showings.db` relative to the API process working directory. Stop the API before deleting the file to reset local data.

## Tests fail

Run the test project directly for detailed output:

```bash
dotnet test tests/PropertyShowingBookingApi.Tests --logger "console;verbosity=detailed"
```

Record the complete exception and stack trace. Do not fix concurrency failures by removing the database guard or by weakening assertions.
