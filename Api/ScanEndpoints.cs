using MedScans.Scans;
using Microsoft.AspNetCore.Mvc;

namespace MedScans.Api;

public static class ScanEndpoints
{
    public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scans");

        group.MapGet("/", async (ScanService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(cancellationToken)));

        group.MapGet("/{id:guid}", async (Guid id, ScanService service, CancellationToken cancellationToken) =>
        {
            var scan = await service.GetByIdAsync(id, cancellationToken);
            return scan is null ? Results.NotFound() : Results.Ok(scan);
        });

        group.MapGet("/{id:guid}/image", async (Guid id, ScanService service, CancellationToken cancellationToken) =>
        {
            var scan = await service.GetEntityAsync(id, cancellationToken);
            return scan is null
                ? Results.NotFound()
                : Results.File(scan.ImageBytes, scan.ContentType, scan.OriginalFileName);
        });

        group.MapPost("/analyze", async (
            [FromForm] IFormFile image,
            [FromForm] Guid? patientId,
            ScanService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await using var stream = image.OpenReadStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);

                var response = await service.AnalyzeAsync(new AnalyzeScanCommand(
                    patientId,
                    image.FileName,
                    string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType,
                    buffer.ToArray()), cancellationToken);

                return Results.Created($"/api/scans/{response.Id}", response);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        }).DisableAntiforgery();

        return app;
    }
}
