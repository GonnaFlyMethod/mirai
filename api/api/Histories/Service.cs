using System.Text.RegularExpressions;
using MedScans.Patients;

namespace MedScans.Histories;

public class HistoryService
{
    // Each record begins with a date on its own line -- that's the only thing needed to find
    // record boundaries. Everything from one match to the next is one record's raw text.
    private static readonly Regex RecordStartPattern = new(
        @"^[ \t]*\d{4}-\d{2}-\d{2}",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Within a record's raw text, the timestamp (digits/punctuation/T/Z, plus any stray OCR
    // whitespace) is immediately followed by the title, which starts with a real word -- so
    // the first run of 2+ letters marks where the timestamp ends and the title begins.
    private static readonly Regex TitleStartPattern = new(@"[A-Za-z]{2,}", RegexOptions.Compiled);

    private readonly IHistoryRepository repository;
    private readonly IPatientRepository patientRepository;
    private readonly IOcrEngine ocrEngine;

    public HistoryService(IHistoryRepository repository, IPatientRepository patientRepository, IOcrEngine ocrEngine)
    {
        this.repository = repository;
        this.patientRepository = patientRepository;
        this.ocrEngine = ocrEngine;
    }

    public async Task<List<HistoryRecord>?> CreateHistory(Guid patientId, byte[] pdfFile, CancellationToken cancellationToken)
    {
        if (await patientRepository.GetByIdAsync(patientId) is null)
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

    public async Task<List<HistoryRecord>?> SearchInHistory(Guid patientId, HistorySearchCriteria criteria)
    {
        if (await patientRepository.GetByIdAsync(patientId) is null)
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

        var starts = RecordStartPattern.Matches(recognizedText);

        for (var i = 0; i < starts.Count; i++)
        {
            var chunkStart = starts[i].Index;
            var chunkEnd = i + 1 < starts.Count ? starts[i + 1].Index : recognizedText.Length;
            var chunk = recognizedText[chunkStart..chunkEnd];

            var titleStart = TitleStartPattern.Match(chunk);
            if (!titleStart.Success)
            {
                continue;
            }

            var dateValue = Regex.Replace(chunk[..titleStart.Index], @"[ \t]", string.Empty);
            var remainder = chunk[titleStart.Index..].Trim();

            if (remainder.Length == 0 || !DateTimeOffset.TryParse(
                    dateValue,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedOffset))
            {
                continue;
            }

            var datetime = parsedOffset.DateTime;

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
