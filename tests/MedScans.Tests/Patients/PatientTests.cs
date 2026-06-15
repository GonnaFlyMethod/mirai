using MedScans.Patients;

namespace MedScans.Tests.Patients;

public sealed class PatientTests
{
    [Fact]
    public void Create_trims_patient_details_and_sets_identity_and_timestamps()
    {
        var before = DateTime.UtcNow;

        var patient = Patient.Create(
            "  Ada  ",
            "  Lovelace  ",
            new DateOnly(1995, 12, 10),
            "  Female  ",
            "  ada@example.com  ",
            "  +48 123 456 789  ",
            "  Warsaw  ");

        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, patient.Id);
        Assert.Equal("Ada", patient.FirstName);
        Assert.Equal("Lovelace", patient.LastName);
        Assert.Equal("Female", patient.Gender);
        Assert.Equal("ada@example.com", patient.Email);
        Assert.Equal("+48 123 456 789", patient.PhoneNumber);
        Assert.Equal("Warsaw", patient.Address);
        Assert.InRange(patient.CreatedAt, before, after);
        Assert.Equal(patient.CreatedAt, patient.UpdatedAt);
    }

    [Theory]
    [InlineData("", "Patient")]
    [InlineData("Patient", "")]
    [InlineData("   ", "Patient")]
    [InlineData("Patient", "   ")]
    public void Create_rejects_patients_without_first_and_last_name(string firstName, string lastName)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Patient.Create(
                firstName,
                lastName,
                new DateOnly(1990, 1, 1),
                "Other",
                "patient@example.com",
                "123",
                "Address"));

        Assert.Equal("Patient first name and last name are required.", exception.Message);
    }
}
