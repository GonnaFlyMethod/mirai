using System.Text.Json;
using MedScans.Patients;

namespace MedScans.Scans;

public sealed record AnalyzeScanCommand(
    Guid? PatientId,
    string OriginalFileName,
    string ContentType,
    byte[] ImageBytes);

public sealed record ScanResponse(
    Guid Id,
    Guid? PatientId,
    string OriginalFileName,
    string ContentType,
    DateTime CreatedAt,
    string PredictedLabel,
    float Confidence,
    IReadOnlyDictionary<string, float> Probabilities,
    string AnalysisStatus,
    string AnalyzerVersion,
    string? ErrorMessage,
    string ImageUrl);

public sealed class ScanService(
    IScanRepository scanRepository,
    IPatientRepository patientRepository,
    IBrainTumorAnalyzer analyzer)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/bmp",
        "image/webp"
    };

    public async Task<IReadOnlyList<ScanResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var scans = await scanRepository.GetAllAsync(cancellationToken);
        return scans.Select(ToResponse).ToList();
    }

    public async Task<BrainScan?> GetEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        return await scanRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<ScanResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var scan = await scanRepository.GetByIdAsync(id, cancellationToken);
        return scan is null ? null : ToResponse(scan);
    }

    public async Task<ScanResponse> AnalyzeAsync(AnalyzeScanCommand command, CancellationToken cancellationToken)
    {
        Validate(command);

        if (command.PatientId is not null && await patientRepository.GetByIdAsync(command.PatientId.Value) is null)
        {
            throw new InvalidOperationException("Selected patient does not exist.");
        }

        BrainTumorAnalysis analysis;
        try
        {
            analysis = await analyzer.AnalyzeAsync(command.ImageBytes, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            analysis = new BrainTumorAnalysis(
                "analysis-failed",
                0,
                new Dictionary<string, float>(),
                "Failed",
                "onnx-resnet50",
                exception.Message);
        }

        var scan = new BrainScan
        {
            Id = Guid.NewGuid(),
            PatientId = command.PatientId,
            OriginalFileName = Path.GetFileName(command.OriginalFileName),
            ContentType = command.ContentType,
            ImageBytes = command.ImageBytes,
            CreatedAt = DateTime.UtcNow,
            PredictedLabel = analysis.PredictedLabel,
            Confidence = analysis.Confidence,
            ProbabilitiesJson = JsonSerializer.Serialize(analysis.Probabilities, JsonOptions),
            AnalysisStatus = analysis.Status,
            AnalyzerVersion = analysis.AnalyzerVersion,
            ErrorMessage = analysis.ErrorMessage
        };

        return ToResponse(await scanRepository.CreateAsync(scan, cancellationToken));
    }

    private static void Validate(AnalyzeScanCommand command)
    {
        if (command.ImageBytes.Length == 0)
        {
            throw new InvalidOperationException("A brain MRI image is required.");
        }

        if (command.ImageBytes.Length > 10 * 1024 * 1024)
        {
            throw new InvalidOperationException("Scan image must be 10 MB or smaller.");
        }

        if (!SupportedContentTypes.Contains(command.ContentType))
        {
            throw new InvalidOperationException("Supported image types are JPEG, PNG, BMP, and WEBP.");
        }
    }

    private static ScanResponse ToResponse(BrainScan scan)
    {
        var probabilities = JsonSerializer.Deserialize<Dictionary<string, float>>(scan.ProbabilitiesJson, JsonOptions)
            ?? new Dictionary<string, float>();

        return new ScanResponse(
            scan.Id,
            scan.PatientId,
            scan.OriginalFileName,
            scan.ContentType,
            scan.CreatedAt,
            scan.PredictedLabel,
            scan.Confidence,
            probabilities,
            scan.AnalysisStatus,
            scan.AnalyzerVersion,
            scan.ErrorMessage,
            $"/api/scans/{scan.Id}/image");
    }
}
