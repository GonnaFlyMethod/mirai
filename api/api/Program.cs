using MedScans.Endpoints;
using MedScans.Histories;
using MedScans.Infrastructure;
using MedScans.Patients;
using MedScans.Scans;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=app.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<IScanRepository, Repository>();
builder.Services.AddScoped<ScanService>();
builder.Services.AddSingleton<IBrainTumorAnalyzer, OnnxBrainTumorAnalyzer>();
builder.Services.AddHttpClient<IOcrEngine, UnlimitedOcrEngine>(client =>
{
    var baseUrl = builder.Configuration["OcrEngine:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});
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

public partial class Program;
