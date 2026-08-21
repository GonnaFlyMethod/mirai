using MedScans.Histories;
using MedScans.Patients;

namespace MedScans.Tests.Patients;

public sealed class PatientHistoryServiceTests
{
    [Fact]
    public async Task CreateHistory_parses_recognized_text_into_records_and_persists_them()
    {
        var patient = Patient.Create(
            "Emily",
            "Carter",
            new DateOnly(1995, 12, 10),
            "Female",
            "emily.carter@example.com",
            "+1 (555) 013-4829",
            "125 Maple Street, Austin, TX 78701");

        var repository = new FakePatientRepository(patient);
        var ocrEngine = FakeOcrEngine.Returning(
            "2026-03-12 Heartattack\nPatient got a heart attack when he took penicillin.\n" +
            "2026-04-01 Checkup\nRoutine checkup, no concerns.");

        var service = new PatientService(repository, ocrEngine);

        var records = await service.CreateHistory(patient.Id, new byte[] { 1, 2, 3 }, CancellationToken.None);

        Assert.NotNull(records);
        Assert.Equal(2, records.Count);
        Assert.Equal("Heartattack", records[0].Title);
        Assert.Equal("Patient got a heart attack when he took penicillin.", records[0].Description);
        Assert.Equal(new DateTime(2026, 3, 12), records[0].Datetime);
        Assert.Equal("Checkup", records[1].Title);
        Assert.Equal(2, repository.CreatedHistoryRecords.Count);
    }

    [Fact]
    public async Task CreateHistory_returns_null_when_patient_does_not_exist()
    {
        var repository = new FakePatientRepository();
        var ocrEngine = FakeOcrEngine.Returning("2026-03-12 Title\nDescription.");
        var service = new PatientService(repository, ocrEngine);

        var result = await service.CreateHistory(Guid.NewGuid(), new byte[] { 1 }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateHistory_throws_when_no_records_can_be_recognized()
    {
        var patient = Patient.Create(
            "Emily",
            "Carter",
            new DateOnly(1995, 12, 10),
            "Female",
            "emily.carter@example.com",
            "+1 (555) 013-4829",
            "125 Maple Street, Austin, TX 78701");

        var repository = new FakePatientRepository(patient);
        var ocrEngine = FakeOcrEngine.Returning("this text has no dates in it at all");
        var service = new PatientService(repository, ocrEngine);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateHistory(patient.Id, new byte[] { 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task SearchInHistory_delegates_criteria_to_repository()
    {
        var patient = Patient.Create(
            "Emily",
            "Carter",
            new DateOnly(1995, 12, 10),
            "Female",
            "emily.carter@example.com",
            "+1 (555) 013-4829",
            "125 Maple Street, Austin, TX 78701");

        var repository = new FakePatientRepository(patient);
        var record = HistoryRecord.Create(patient.Id, new DateTime(2026, 3, 12), "Heartattack", "Details");
        await repository.CreateHistoryAsync(record);

        var service = new PatientService(repository, FakeOcrEngine.Returning(string.Empty));

        var results = await service.SearchInHistory(patient.Id, new HistorySearchCriteria(null, "Heartattack", null));

        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal(record.Id, results![0].Id);
    }

    [Fact]
    public async Task GetHistory_returns_null_when_patient_does_not_exist()
    {
        var repository = new FakePatientRepository();
        var service = new PatientService(repository, FakeOcrEngine.Returning(string.Empty));

        var result = await service.GetHistory(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchInHistory_returns_null_when_patient_does_not_exist()
    {
        var repository = new FakePatientRepository();
        var service = new PatientService(repository, FakeOcrEngine.Returning(string.Empty));

        var result = await service.SearchInHistory(Guid.NewGuid(), new HistorySearchCriteria(null, "Heartattack", null));

        Assert.Null(result);
    }

    private sealed class FakeOcrEngine : IOcrEngine
    {
        private readonly string _text;

        private FakeOcrEngine(string text)
        {
            _text = text;
        }

        public static FakeOcrEngine Returning(string text) => new(text);

        public Task<string> RecognizeAsync(byte[] pdfBytes, CancellationToken cancellationToken)
        {
            return Task.FromResult(_text);
        }
    }

    private sealed class FakePatientRepository : IPatientRepository
    {
        private readonly List<Patient> _patients;
        private readonly List<HistoryRecord> _histories = new();

        public FakePatientRepository(params Patient[] patients)
        {
            _patients = patients.ToList();
        }

        public IReadOnlyList<HistoryRecord> CreatedHistoryRecords => _histories;

        public Task<List<Patient>> GetAllAsync() => Task.FromResult(_patients.ToList());

        public Task<Patient?> GetByIdAsync(Guid id) =>
            Task.FromResult(_patients.SingleOrDefault(patient => patient.Id == id));

        public Task<Patient> CreateAsync(Patient patient)
        {
            _patients.Add(patient);
            return Task.FromResult(patient);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            var patient = _patients.SingleOrDefault(candidate => candidate.Id == id);
            if (patient is null)
            {
                return Task.FromResult(false);
            }

            _patients.Remove(patient);
            return Task.FromResult(true);
        }

        public Task<HistoryRecord> CreateHistoryAsync(HistoryRecord record)
        {
            _histories.Add(record);
            return Task.FromResult(record);
        }

        public Task<List<HistoryRecord>> CreateHistoriesAsync(List<HistoryRecord> records)
        {
            _histories.AddRange(records);
            return Task.FromResult(records);
        }

        public Task<List<HistoryRecord>> GetHistoryAsync(Guid patientId) =>
            Task.FromResult(_histories.Where(record => record.PatientId == patientId).ToList());

        public Task<List<HistoryRecord>> SearchInHistoryAsync(Guid patientId, HistorySearchCriteria criteria)
        {
            var candidates = _histories.Where(record => record.PatientId == patientId);

            if (!string.IsNullOrWhiteSpace(criteria.Title) || !string.IsNullOrWhiteSpace(criteria.Description))
            {
                candidates = candidates.Where(record => HistorySimilarity.Score(record, criteria) > 0);
            }

            return Task.FromResult(candidates.ToList());
        }
    }
}
