using MedScans.Histories;
using Microsoft.AspNetCore.Http.Features;

namespace MedScans.Endpoints;

public static class HistoryEndpoints
{
    private const long DefaultMaxUploadSizeBytes = 20 * 1024 * 1024;
    private const long MultipartOverheadBytes = 64 * 1024;

    public static IEndpointRouteBuilder MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patients/{id:guid}/histories");

        group.MapGet("/", async (
            Guid id,
            string? datetime,
            string? title,
            string? description,
            HistoryService service) =>
        {
            DateTime? parsedDatetime = null;
            if (!string.IsNullOrWhiteSpace(datetime))
            {
                if (!DateTimeOffset.TryParse(
                        datetime,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var value))
                {
                    return Results.BadRequest(new { error = "datetime must be a valid ISO8601 date/time." });
                }

                parsedDatetime = value.DateTime;
            }

            var criteria = new HistorySearchCriteria(parsedDatetime, title, description);
            var results = await service.SearchInHistory(id, criteria);

            return results is null ? Results.NotFound() : Results.Ok(results.Select(ToResponse));
        });

        group.MapPost("/", async (
            Guid id,
            HttpRequest request,
            HistoryService service,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            var maxUploadSizeBytes = configuration.GetValue<long?>("HistoryUpload:MaxSizeBytes") ?? DefaultMaxUploadSizeBytes;

            try
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { error = "Expected a multipart form upload." });
                }

                var maxRequestBodySizeFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (maxRequestBodySizeFeature is { IsReadOnly: false })
                {
                    maxRequestBodySizeFeature.MaxRequestBodySize = maxUploadSizeBytes + MultipartOverheadBytes;
                }

                var form = await request.ReadFormAsync(cancellationToken);
                var file = form.Files.GetFile("file");

                if (file is null)
                {
                    return Results.BadRequest(new { error = "A PDF file with the patient's history is required." });
                }

                if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "Only PDF files are supported." });
                }

                if (file.Length > maxUploadSizeBytes)
                {
                    return Results.BadRequest(new { error = $"The PDF file must not exceed {maxUploadSizeBytes / (1024 * 1024)} MB." });
                }

                await using var stream = file.OpenReadStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);

                var created = await service.CreateHistory(id, buffer.ToArray(), cancellationToken);

                if (created is null)
                {
                    return Results.NotFound();
                }

                return Results.Created($"/api/patients/{id}/histories", created.Select(ToResponse));
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
            catch (BadHttpRequestException)
            {
                return Results.BadRequest(new { error = $"The PDF file must not exceed {maxUploadSizeBytes / (1024 * 1024)} MB." });
            }
        });

        return app;
    }

    private static object ToResponse(HistoryRecord record) => new
    {
        record.Id,
        record.PatientId,
        record.Datetime,
        record.Title,
        record.Description
    };
}
