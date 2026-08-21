using MedScans.Histories;
using MedScans.Patients;

namespace MedScans.Endpoints;

public static class HistoryEndpoints
{
    public static IEndpointRouteBuilder MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patients/{id:guid}/histories");

        group.MapGet("/", async (
            Guid id,
            string? datetime,
            string? title,
            string? description,
            PatientService service) =>
        {
            if (datetime is null && title is null && description is null)
            {
                return Results.Ok((await service.GetHistory(id)).Select(ToResponse));
            }

            DateTime? parsedDatetime = null;
            if (!string.IsNullOrWhiteSpace(datetime))
            {
                if (!DateTime.TryParse(
                        datetime,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var value))
                {
                    return Results.BadRequest(new { error = "datetime must be a valid ISO8601 date/time." });
                }

                parsedDatetime = value;
            }

            var criteria = new HistorySearchCriteria(parsedDatetime, title, description);
            var results = await service.SearchInHistory(id, criteria);

            return Results.Ok(results.Select(ToResponse));
        });

        group.MapPost("/", async (
            Guid id,
            HttpRequest request,
            PatientService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { error = "Expected a multipart form upload." });
                }

                var form = await request.ReadFormAsync(cancellationToken);
                var file = form.Files.GetFile("file");

                if (file is null)
                {
                    return Results.BadRequest(new { error = "A PDF file with the patient's history is required." });
                }

                await using var stream = file.OpenReadStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);

                var created = await service.CreateHistory(id, buffer.ToArray(), cancellationToken);

                return Results.Created($"/api/patients/{id}/histories", created.Select(ToResponse));
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
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
