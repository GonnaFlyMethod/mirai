using Docnet.Core;
using Docnet.Core.Converters;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tesseract;

namespace MedScans.Histories;

// Tesseract (charlesw/tesseract wrapper around the native tesseract50/leptonica libraries)
// runs fully in-process on CPU: no GPU, no remote service, and none of the native
// runtime/model-export version-compatibility crashes Sdcb.PaddleOCR hit (see prior commits/
// conversation history for that attempt -- PP-OCRv5's detection model threw a native
// fused_conv2d/oneDNN error across every device backend tried; abandoned as an upstream bug).
// PDF pages are rasterized page-by-page with Docnet.Core (a PDFium wrapper). Docnet renders
// with a transparent background, so pages are composited onto white first
// (NaiveTransparencyRemover) -- otherwise black text on a transparent background collapses
// into an invisible black-on-black image once alpha is dropped. Pix.LoadFromMemory expects an
// encoded image format, not a raw pixel buffer, so the composited BGRA bytes are re-encoded as
// PNG via ImageSharp (already a project dependency for OnnxBrainTumorAnalyzer) before handing
// them to Tesseract. A fresh TesseractEngine is created per call rather than cached/shared,
// since TesseractEngine isn't safe for concurrent use and loading eng.traineddata is cheap
// (a few MB) compared to Paddle's native model graph construction.
public sealed class TesseractOcrEngine : IOcrEngine
{
    // Docnet's PageDimensions(scale) multiplies the PDF's native point-based size, which
    // preserves aspect ratio (unlike a fixed pixel width/height, which would distort text).
    // ~2.78 approximates 200 DPI (200/72). Revisit if recognition quality on real scans is poor.
    private const double RenderScale = 200.0 / 72.0;

    private readonly string tessDataPath;

    public TesseractOcrEngine(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["OcrEngine:TessDataPath"] ?? "tessdata";
        tessDataPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }

    public Task<string> RecognizeAsync(byte[] pdfBytes, CancellationToken cancellationToken)
    {
        if (pdfBytes.Length == 0)
        {
            throw new InvalidOperationException("PDF file is required for OCR recognition.");
        }

        if (!File.Exists(Path.Combine(tessDataPath, "eng.traineddata")))
        {
            throw new InvalidOperationException($"Tesseract language data was not found at '{tessDataPath}'.");
        }

        var pageTexts = new List<string>();

        using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.LstmOnly);
        using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(RenderScale));

        for (var i = 0; i < docReader.GetPageCount(); i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var pageReader = docReader.GetPageReader(i);
            var pngBytes = ToPng(
                pageReader.GetImage(new NaiveTransparencyRemover(255, 255, 255)),
                pageReader.GetPageWidth(),
                pageReader.GetPageHeight());

            using var pix = Pix.LoadFromMemory(pngBytes);
            using var page = engine.Process(pix);
            pageTexts.Add(page.GetText());
        }

        return Task.FromResult(string.Join("\n\n", pageTexts));
    }

    private static byte[] ToPng(byte[] bgraBytes, int width, int height)
    {
        using var image = Image.LoadPixelData<Bgra32>(bgraBytes, width, height);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
}
