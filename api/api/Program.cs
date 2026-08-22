using System.Text.RegularExpressions;
using MedScans.Endpoints;
using MedScans.Histories;
using MedScans.Infrastructure;
using MedScans.Patients;
using MedScans.Scans;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=app.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();
builder.Services.AddScoped<HistoryService>();
builder.Services.AddScoped<IScanRepository, Repository>();
builder.Services.AddScoped<ScanService>();
builder.Services.AddSingleton<IBrainTumorAnalyzer, OnnxBrainTumorAnalyzer>();
builder.Services.AddSingleton<IOcrEngine, TesseractOcrEngine>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await EnsureMissingTablesAsync(db);
}

app.UseCors("Frontend");
app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapPatientEndpoints();
app.MapScanEndpoints();
app.MapHistoryEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

static async Task EnsureMissingTablesAsync(AppDbContext db)
{
    var connection = (SqliteConnection)db.Database.GetDbConnection();
    var wasClosed = connection.State != System.Data.ConnectionState.Open;
    if (wasClosed)
    {
        await connection.OpenAsync();
    }

    try
    {
        var createScript = db.Database.GenerateCreateScript();
        var tableStatements = Regex.Split(createScript.Trim(), @"\r?\n\r?\n");

        foreach (var statement in tableStatements)
        {
            var tableNameMatch = Regex.Match(statement, "CREATE TABLE \"(?<name>[^\"]+)\"");
            if (!tableNameMatch.Success)
            {
                continue;
            }

            var tableName = tableNameMatch.Groups["name"].Value;

            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name";
            checkCommand.Parameters.AddWithValue("$name", tableName);
            var alreadyExists = await checkCommand.ExecuteScalarAsync() is not null;

            if (alreadyExists)
            {
                continue;
            }

            await using var createCommand = connection.CreateCommand();
            createCommand.CommandText = statement;
            await createCommand.ExecuteNonQueryAsync();
        }
    }
    finally
    {
        if (wasClosed)
        {
            await connection.CloseAsync();
        }
    }
}

public partial class Program;
