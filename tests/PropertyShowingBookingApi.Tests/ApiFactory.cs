using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace PropertyShowingBookingApi.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "Test_" + Guid.NewGuid().ToString("N");
    private SqliteConnection? _keepAliveConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString =
            $"Data Source={_databaseName};Mode=Memory;Cache=Shared";

        // Keep the shared in-memory SQLite database alive.
        _keepAliveConnection = new SqliteConnection(connectionString);
        _keepAliveConnection.Open();

        // Configure the application to use the test database.
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = connectionString
                });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _keepAliveConnection?.Dispose();
        }
    }
}
