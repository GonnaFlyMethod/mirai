using MedScans.Patients;

namespace MedScans.Tests.Patients;

public sealed class PatientTests
{
    [Fact]
    public void Create_trims_patient_details_and_sets_identity_and_timestamps()
    {
        var before = DateTime.UtcNow;

        var patient = Patient.Create(
            "  Emily  ",
            "  Carter  ",
            new DateOnly(1995, 12, 10),
            "  Female  ",
            "  emily.carter@example.com  ",
            "  +1 (555) 013-4829  ",
            "  125 Maple Street, Austin, TX 78701  ");

        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, patient.Id);
        Assert.Equal("Emily", patient.FirstName);
        Assert.Equal("Carter", patient.LastName);
        Assert.Equal("Female", patient.Gender);
        Assert.Equal("emily.carter@example.com", patient.Email);
        Assert.Equal("+1 (555) 013-4829", patient.PhoneNumber);
        Assert.Equal("125 Maple Street, Austin, TX 78701", patient.Address);
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
