using System.Net;
using System.Net.Http.Json;
using MedScans.Histories;
using MedScans.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MedScans.Tests.Infrastructure;

public sealed class DatabaseUpgradeTests
{
    [Fact]
    public async Task Startup_adds_missing_tables_to_a_pre_existing_database()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"database-upgrade-tests-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        // Simulate an install created before the Histories table existed: build every
        // table except Histories directly, bypassing EnsureCreatedAsync's all-or-nothing check.
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString).Options;
        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE Histories");
        }

        using var factory = new UpgradeWebApplicationFactory(connectionString);
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Emily",
            lastName = "Carter",
            dateOfBirth = "1995-12-10",
            gender = "Female",
            email = $"emily.{Guid.NewGuid():N}@example.com",
            phoneNumber = "+1 (555) 013-4829",
            address = "125 Maple Street, Austin, TX 78701"
        });
        createResponse.EnsureSuccessStatusCode();
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        var historyResponse = await client.GetAsync($"/api/patients/{patient!.Id}/histories");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var records = await historyResponse.Content.ReadFromJsonAsync<List<HistoryRecord>>();
        Assert.NotNull(records);
        Assert.Empty(records!);
    }

    private sealed record PatientResponse(Guid Id);

    private sealed class UpgradeWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = connectionString
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IOcrEngine>();
                services.AddSingleton<IOcrEngine>(new NullOcrEngine());
            });
        }
    }

    private sealed class NullOcrEngine : IOcrEngine
    {
        public Task<string> RecognizeAsync(byte[] pdfBytes, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }
    }
}
