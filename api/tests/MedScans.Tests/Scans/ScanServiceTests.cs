using MedScans.Patients;
using MedScans.Scans;

namespace MedScans.Tests.Scans;

public sealed class ScanServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_persists_scan_with_sanitized_file_name_and_analyzer_result()
    {
        var scanRepository = new FakeScanRepository();
        var analyzer = FakeBrainTumorAnalyzer.Returning(new BrainTumorAnalysis(
            "glioma",
            0.91f,
            new Dictionary<string, float>
            {
                ["glioma"] = 0.91f,
                ["meningioma"] = 0.05f,
                ["notumor"] = 0.02f,
                ["pituitary"] = 0.02f
            },
            "Completed",
            "test-analyzer"));

        var service = new ScanService(scanRepository, new FakePatientRepository(), analyzer);
        var imageBytes = new byte[] { 1, 2, 3 };

        var response = await service.AnalyzeAsync(new AnalyzeScanCommand(
            null,
            "unsafe-folder/scan.png",
            "image/png",
            imageBytes), CancellationToken.None);

        var persistedScan = Assert.Single(scanRepository.CreatedScans);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(response.Id, persistedScan.Id);
        Assert.Equal("scan.png", response.OriginalFileName);
        Assert.Equal("scan.png", persistedScan.OriginalFileName);
        Assert.Equal("image/png", response.ContentType);
        Assert.Same(imageBytes, persistedScan.ImageBytes);
        Assert.Equal("glioma", response.PredictedLabel);
        Assert.Equal(0.91f, response.Confidence, precision: 3);
        Assert.Equal("Completed", response.AnalysisStatus);
        Assert.Equal("test-analyzer", response.AnalyzerVersion);
        Assert.Equal("/api/scans/" + response.Id + "/image", response.ImageUrl);
        Assert.Equal(4, response.Probabilities.Count);
        Assert.Equal(1, analyzer.CallCount);
    }

    [Fact]
    public async Task AnalyzeAsync_rejects_unknown_patient_before_running_analysis()
    {
        var scanRepository = new FakeScanRepository();
        var analyzer = FakeBrainTumorAnalyzer.Returning(SuccessfulAnalysis());
        var service = new ScanService(scanRepository, new FakePatientRepository(), analyzer);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnalyzeAsync(new AnalyzeScanCommand(
                Guid.NewGuid(),
                "scan.png",
                "image/png",
                new byte[] { 1 }), CancellationToken.None));

        Assert.Equal("Selected patient does not exist.", exception.Message);
        Assert.Empty(scanRepository.CreatedScans);
        Assert.Equal(0, analyzer.CallCount);
    }

    [Fact]
    public async Task AnalyzeAsync_persists_failed_analysis_when_analyzer_fails()
    {
        var scanRepository = new FakeScanRepository();
        var analyzer = FakeBrainTumorAnalyzer.Throwing(new InvalidDataException("Invalid MRI bytes."));
        var service = new ScanService(scanRepository, new FakePatientRepository(), analyzer);

        var response = await service.AnalyzeAsync(new AnalyzeScanCommand(
            null,
            "scan.png",
            "image/png",
            new byte[] { 1, 2, 3 }), CancellationToken.None);

        Assert.Single(scanRepository.CreatedScans);
        Assert.Equal("analysis-failed", response.PredictedLabel);
        Assert.Equal(0, response.Confidence);
        Assert.Empty(response.Probabilities);
        Assert.Equal("Failed", response.AnalysisStatus);
        Assert.Equal("onnx-resnet50", response.AnalyzerVersion);
        Assert.Equal("Invalid MRI bytes.", response.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeAsync_does_not_swallow_cancellation()
    {
        var scanRepository = new FakeScanRepository();
        var analyzer = FakeBrainTumorAnalyzer.Throwing(new OperationCanceledException());
        var service = new ScanService(scanRepository, new FakePatientRepository(), analyzer);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.AnalyzeAsync(new AnalyzeScanCommand(
                null,
                "scan.png",
                "image/png",
                new byte[] { 1 }), CancellationToken.None));

        Assert.Empty(scanRepository.CreatedScans);
    }

    [Fact]
    public async Task AnalyzeAsync_rejects_empty_images()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnalyzeAsync(new AnalyzeScanCommand(
                null,
                "scan.png",
                "image/png",
                Array.Empty<byte>()), CancellationToken.None));

        Assert.Equal("A brain MRI image is required.", exception.Message);
    }

    [Fact]
    public async Task AnalyzeAsync_rejects_images_larger_than_ten_megabytes()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnalyzeAsync(new AnalyzeScanCommand(
                null,
                "scan.png",
                "image/png",
                new byte[(10 * 1024 * 1024) + 1]), CancellationToken.None));

        Assert.Equal("Scan image must be 10 MB or smaller.", exception.Message);
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    public async Task AnalyzeAsync_rejects_unsupported_content_types(string contentType)
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnalyzeAsync(new AnalyzeScanCommand(
                null,
                "scan.png",
                contentType,
                new byte[] { 1 }), CancellationToken.None));

        Assert.Equal("Supported image types are JPEG, PNG, BMP, and WEBP.", exception.Message);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/bmp")]
    [InlineData("image/webp")]
    public async Task AnalyzeAsync_accepts_supported_content_types_case_insensitively(string contentType)
    {
        var scanRepository = new FakeScanRepository();
        var service = CreateService(scanRepository);

        await service.AnalyzeAsync(new AnalyzeScanCommand(
            null,
            "scan.png",
            contentType.ToUpperInvariant(),
            new byte[] { 1 }), CancellationToken.None);

        Assert.Single(scanRepository.CreatedScans);
    }

    private static ScanService CreateService(FakeScanRepository? scanRepository = null)
    {
        return new ScanService(
            scanRepository ?? new FakeScanRepository(),
            new FakePatientRepository(),
            FakeBrainTumorAnalyzer.Returning(SuccessfulAnalysis()));
    }

    private static BrainTumorAnalysis SuccessfulAnalysis()
    {
        return new BrainTumorAnalysis(
            "notumor",
            0.99f,
            new Dictionary<string, float> { ["notumor"] = 0.99f },
            "Completed",
            "test-analyzer");
    }

    private sealed class FakeScanRepository : IScanRepository
    {
        private readonly List<BrainScan> _scans = new();

        public IReadOnlyList<BrainScan> CreatedScans => _scans;

        public Task<List<BrainScan>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_scans.ToList());
        }

        public Task<BrainScan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_scans.SingleOrDefault(scan => scan.Id == id));
        }

        public Task<BrainScan> CreateAsync(BrainScan scan, CancellationToken cancellationToken)
        {
            _scans.Add(scan);
            return Task.FromResult(scan);
        }
    }

    private sealed class FakePatientRepository : IPatientRepository
    {
        private readonly List<Patient> _patients;

        public FakePatientRepository(params Patient[] patients)
        {
            _patients = patients.ToList();
        }

        public Task<List<Patient>> GetAllAsync()
        {
            return Task.FromResult(_patients.ToList());
        }

        public Task<Patient?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_patients.SingleOrDefault(patient => patient.Id == id));
        }

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
    }

    private sealed class FakeBrainTumorAnalyzer : IBrainTumorAnalyzer
    {
        private readonly BrainTumorAnalysis? _analysis;
        private readonly Exception? _exception;

        private FakeBrainTumorAnalyzer(BrainTumorAnalysis? analysis, Exception? exception)
        {
            _analysis = analysis;
            _exception = exception;
        }

        public int CallCount { get; private set; }

        public static FakeBrainTumorAnalyzer Returning(BrainTumorAnalysis analysis)
        {
            return new FakeBrainTumorAnalyzer(analysis, null);
        }

        public static FakeBrainTumorAnalyzer Throwing(Exception exception)
        {
            return new FakeBrainTumorAnalyzer(null, exception);
        }

        public Task<BrainTumorAnalysis> AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken)
        {
            CallCount++;

            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_analysis!);
        }
    }
}
