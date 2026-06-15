namespace MedScans.Patients;

public class Patient
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static Patient Create(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string gender,
        string email,
        string phoneNumber,
        string address)
    {
        firstName = firstName.Trim();
        lastName = lastName.Trim();

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            throw new InvalidOperationException("Patient first name and last name are required.");
        }

        var now = DateTime.UtcNow;

        return new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            Gender = gender.Trim(),
            Email = email.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Address = address.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
