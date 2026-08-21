using System.Net.Http.Json;
using System.Text.Json;

namespace MedScans.Histories;

// Baidu's Unlimited-OCR (https://github.com/baidu/Unlimited-OCR) ships a Python-only
// implementation with no official C#/.NET bindings. The upstream project's recommended
// deployment path is to serve it behind an OpenAI-compatible HTTP API (via vLLM/SGLang),
// so rather than embedding a Python runtime in this process, this client talks to that
// HTTP endpoint. Deploy the OCR model as a separate service and point OcrEngine:BaseUrl
// at it (see appsettings for configuration). This choice is a candidate for review,
// same as the search approach below.
public sealed class UnlimitedOcrEngine : IOcrEngine
{
    private readonly HttpClient httpClient;
    private readonly string model;

    public UnlimitedOcrEngine(HttpClient httpClient, IConfiguration configuration)
    {
        this.httpClient = httpClient;
        model = configuration["OcrEngine:Model"] ?? "unlimited-ocr";
    }

    public async Task<string> RecognizeAsync(byte[] pdfBytes, CancellationToken cancellationToken)
    {
        if (pdfBytes.Length == 0)
        {
            throw new InvalidOperationException("PDF file is required for OCR recognition.");
        }

        var base64Pdf = Convert.ToBase64String(pdfBytes);

        var request = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Recognize all text, formulas and captions from this document, in reading order." },
                        new { type = "file", file = new { file_data = $"data:application/pdf;base64,{base64Pdf}" } }
                    }
                }
            }
        };

        using var response = await httpClient.PostAsJsonAsync("/v1/chat/completions", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}
