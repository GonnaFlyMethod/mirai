namespace MedScans.Scans;

public sealed record BrainTumorAnalysis(
    string PredictedLabel,
    float Confidence,
    IReadOnlyDictionary<string, float> Probabilities,
    string Status,
    string AnalyzerVersion,
    string? ErrorMessage = null);

public interface IBrainTumorAnalyzer
{
    Task<BrainTumorAnalysis> AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken);
}
