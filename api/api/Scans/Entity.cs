namespace MedScans.Scans;

public sealed class BrainScan
{
    public Guid Id { get; set; }

    public Guid? PatientId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; }

    public string PredictedLabel { get; set; } = string.Empty;

    public float Confidence { get; set; }

    public string ProbabilitiesJson { get; set; } = "{}";

    public string AnalysisStatus { get; set; } = string.Empty;

    public string AnalyzerVersion { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}
