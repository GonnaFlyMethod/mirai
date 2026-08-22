namespace MedScans.Histories;

public interface IOcrEngine
{
    Task<string> RecognizeAsync(byte[] pdfBytes, CancellationToken cancellationToken);
}
