using MedScans.Scans;

namespace MedScans.Endpoints;

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
            HttpRequest request,
            ScanService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { error = "Expected a multipart form upload." });
                }

                var form = await request.ReadFormAsync(cancellationToken);
                var image = form.Files.GetFile("image");

                if (image is null)
                {
                    return Results.BadRequest(new { error = "A brain MRI image is required." });
                }

                Guid? patientId = null;
                var patientIdValue = form["patientId"].FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(patientIdValue))
                {
                    if (!Guid.TryParse(patientIdValue, out var parsedPatientId))
                    {
                        return Results.BadRequest(new { error = "Patient id must be a valid GUID." });
                    }

                    patientId = parsedPatientId;
                }

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
        });

        return app;
    }
}
