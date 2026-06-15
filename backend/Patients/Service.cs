namespace MedScans.Patients;

public sealed record CreatePatientRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string Email,
    string PhoneNumber,
    string Address);

public class PatientService(IPatientRepository repository)
{
    public async Task<List<Patient>> GetAll()
    {
        return await repository.GetAllAsync();
    }

    public async Task<Patient?> GetById(Guid id)
    {
        return await repository.GetByIdAsync(id);
    }

    public async Task<Patient> Create(CreatePatientRequest request)
    {
        var patient = Patient.Create(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.Email,
            request.PhoneNumber,
            request.Address);

        return await repository.CreateAsync(patient);
    }

    public async Task<bool> Delete(Guid id)
    {
        return await repository.DeleteAsync(id);
    }
}
