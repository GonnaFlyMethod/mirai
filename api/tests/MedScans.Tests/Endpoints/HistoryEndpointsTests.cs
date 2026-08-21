using System.Net;
using System.Net.Http.Json;
using MedScans.Histories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MedScans.Tests.Endpoints;

public sealed class HistoryEndpointsTests : IClassFixture<HistoryEndpointsTests.HistoryWebApplicationFactory>
{
    private readonly HistoryWebApplicationFactory factory;

    public HistoryEndpointsTests(HistoryWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Post_then_get_returns_recognized_history_records()
    {
        var client = factory.CreateClient();
        var patientId = await CreatePatient(client);

        factory.OcrEngine.Text =
            "2026-03-12 Heartattack\nPatient got a heart attack when he took penicillin.\n" +
            "2026-04-01 Checkup\nRoutine checkup, no concerns.";

        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "history.pdf" }
        };

        var postResponse = await client.PostAsync($"/api/patients/{patientId}/histories", form);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/patients/{patientId}/histories");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var records = await getResponse.Content.ReadFromJsonAsync<List<HistoryRecordResponse>>();
        Assert.NotNull(records);
        Assert.Equal(2, records!.Count);
        Assert.Contains(records, record => record.Title == "Heartattack");
    }

    [Fact]
    public async Task Get_with_title_query_param_returns_only_matching_records()
    {
        var client = factory.CreateClient();
        var patientId = await CreatePatient(client);

        factory.OcrEngine.Text =
            "2026-03-12 Heartattack\nPatient got a heart attack when he took penicillin.\n" +
            "2026-04-01 Checkup\nRoutine checkup, no concerns.";

        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(new byte[] { 1 }), "file", "history.pdf" }
        };
        await client.PostAsync($"/api/patients/{patientId}/histories", form);

        var searchResponse = await client.GetAsync($"/api/patients/{patientId}/histories?title=Heartattack");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var records = await searchResponse.Content.ReadFromJsonAsync<List<HistoryRecordResponse>>();
        Assert.NotNull(records);
        Assert.Single(records!);
        Assert.Equal("Heartattack", records![0].Title);
    }

    [Fact]
    public async Task Post_without_file_returns_bad_request()
    {
        var client = factory.CreateClient();
        var patientId = await CreatePatient(client);

        using var form = new MultipartFormDataContent
        {
            { new StringContent("no file here"), "note" }
        };

        var response = await client.PostAsync($"/api/patients/{patientId}/histories", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_with_invalid_datetime_returns_bad_request()
    {
        var client = factory.CreateClient();
        var patientId = await CreatePatient(client);

        var response = await client.GetAsync($"/api/patients/{patientId}/histories?datetime=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<Guid> CreatePatient(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Emily",
            lastName = "Carter",
            dateOfBirth = "1995-12-10",
            gender = "Female",
            email = $"emily.{Guid.NewGuid():N}@example.com",
            phoneNumber = "+1 (555) 013-4829",
            address = "125 Maple Street, Austin, TX 78701"
        });

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<PatientResponse>();
        return created!.Id;
    }

    private sealed record PatientResponse(Guid Id);

    private sealed record HistoryRecordResponse(Guid Id, Guid PatientId, DateTime Datetime, string Title, string Description);

    public sealed class HistoryWebApplicationFactory : WebApplicationFactory<Program>
    {
        public FakeOcrEngine OcrEngine { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"history-endpoints-tests-{Guid.NewGuid():N}.db");

            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = $"Data Source={dbPath}"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IOcrEngine>();
                services.AddSingleton<IOcrEngine>(OcrEngine);
            });
        }
    }

    public sealed class FakeOcrEngine : IOcrEngine
    {
        public string Text { get; set; } = string.Empty;

        public Task<string> RecognizeAsync(byte[] pdfBytes, CancellationToken cancellationToken)
        {
            return Task.FromResult(Text);
        }
    }
}
