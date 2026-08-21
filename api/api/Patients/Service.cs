using System.Text.RegularExpressions;
using MedScans.Histories;

namespace MedScans.Patients;

public sealed record CreatePatientRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string Email,
    string PhoneNumber,
    string Address);

public class PatientService
{
    private static readonly Regex DatetimePattern = new(
        @"\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2})?)?",
        RegexOptions.Compiled);

    private readonly IPatientRepository repository;
    private readonly IOcrEngine ocrEngine;

    public PatientService(IPatientRepository repository, IOcrEngine ocrEngine)
    {
        this.repository = repository;
        this.ocrEngine = ocrEngine;
    }

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

    public async Task<List<HistoryRecord>?> CreateHistory(Guid patientId, byte[] pdfFile, CancellationToken cancellationToken)
    {
        if (await repository.GetByIdAsync(patientId) is null)
        {
            return null;
        }

        if (pdfFile.Length == 0)
        {
            throw new InvalidOperationException("A PDF file with the patient's history is required.");
        }

        var recognizedText = await Recognize(pdfFile, cancellationToken);
        var records = ParseRecognizedText(patientId, recognizedText);

        if (records.Count == 0)
        {
            throw new InvalidOperationException("No history records could be recognized from the uploaded file.");
        }

        return await repository.CreateHistoriesAsync(records);
    }

    public async Task<List<HistoryRecord>?> GetHistory(Guid patientId)
    {
        if (await repository.GetByIdAsync(patientId) is null)
        {
            return null;
        }

        return await repository.GetHistoryAsync(patientId);
    }

    public async Task<List<HistoryRecord>?> SearchInHistory(Guid patientId, HistorySearchCriteria criteria)
    {
        if (await repository.GetByIdAsync(patientId) is null)
        {
            return null;
        }

        return await repository.SearchInHistoryAsync(patientId, criteria);
    }

    private async Task<string> Recognize(byte[] pdfFile, CancellationToken cancellationToken)
    {
        return await ocrEngine.RecognizeAsync(pdfFile, cancellationToken);
    }

    private static List<HistoryRecord> ParseRecognizedText(Guid patientId, string recognizedText)
    {
        var records = new List<HistoryRecord>();

        if (string.IsNullOrWhiteSpace(recognizedText))
        {
            return records;
        }

        var matches = DatetimePattern.Matches(recognizedText);

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var chunkEnd = i + 1 < matches.Count ? matches[i + 1].Index : recognizedText.Length;
            var remainder = recognizedText[(match.Index + match.Length)..chunkEnd].Trim();

            if (remainder.Length == 0 || !DateTime.TryParse(
                    match.Value,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var datetime))
            {
                continue;
            }

            var titleBreak = remainder.IndexOf('\n');
            string title;
            string description;

            if (titleBreak >= 0)
            {
                title = remainder[..titleBreak].Trim();
                description = remainder[(titleBreak + 1)..].Trim();
            }
            else
            {
                var spaceIndex = remainder.IndexOf(' ');
                title = spaceIndex >= 0 ? remainder[..spaceIndex].Trim() : remainder;
                description = spaceIndex >= 0 ? remainder[(spaceIndex + 1)..].Trim() : string.Empty;
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            records.Add(HistoryRecord.Create(patientId, datetime, title, description));
        }

        return records;
    }
}
