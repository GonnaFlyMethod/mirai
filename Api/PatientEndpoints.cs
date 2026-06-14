using MedScans.Patients;

namespace MedScans.Api;

public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patients");

        group.MapGet("/", async (PatientService service) =>
            Results.Ok((await service.GetAll()).Select(ToResponse)));

        group.MapGet("/{id:guid}", async (Guid id, PatientService service) =>
        {
            var patient = await service.GetById(id);
            return patient is null ? Results.NotFound() : Results.Ok(ToResponse(patient));
        });

        group.MapPost("/", async (CreatePatientRequest request, PatientService service) =>
        {
            try
            {
                var created = await service.Create(request);
                return Results.Created($"/api/patients/{created.Id}", ToResponse(created));
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });

        group.MapDelete("/{id:guid}", async (Guid id, PatientService service) =>
            await service.Delete(id) ? Results.NoContent() : Results.NotFound());

        return app;
    }

    private static object ToResponse(Patient patient) => new
    {
        patient.Id,
        patient.FirstName,
        patient.LastName,
        patient.DateOfBirth,
        patient.Gender,
        patient.Email,
        patient.PhoneNumber,
        patient.Address,
        patient.CreatedAt,
        patient.UpdatedAt
    };
}
