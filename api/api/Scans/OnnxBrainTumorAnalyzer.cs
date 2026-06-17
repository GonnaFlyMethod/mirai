using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MedScans.Scans;

public sealed class OnnxBrainTumorAnalyzer : IBrainTumorAnalyzer, IDisposable
{
    private static readonly string[] Labels = { "glioma", "meningioma", "notumor", "pituitary" };
    private readonly string _modelPath;
    private readonly object _sessionLock = new();
    private InferenceSession? _session;

    public OnnxBrainTumorAnalyzer(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["BrainTumorModel:OnnxPath"] ?? "Models/brain-tumor-resnet50.onnx";
        _modelPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }

    public Task<BrainTumorAnalysis> AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_modelPath))
        {
            return Task.FromResult(new BrainTumorAnalysis(
                "model-unavailable",
                0,
                new Dictionary<string, float>(),
                "ModelUnavailable",
                "onnx-resnet50",
                $"Model file was not found at '{_modelPath}'. Train and export the model with ModelTraining/train_resnet50.py."));
        }

        var session = GetSession();
        var inputName = session.InputMetadata.Keys.First();
        var outputName = session.OutputMetadata.Keys.First();
        var input = Preprocess(imageBytes);

        var inputValue = NamedOnnxValue.CreateFromTensor(inputName, input);
        using var results = session.Run(new[] { inputValue });
        var output = results.First(result => result.Name == outputName).AsEnumerable<float>().ToArray();
        var probabilities = Softmax(output);
        var bestIndex = probabilities
            .Select((score, index) => new { score, index })
            .OrderByDescending(item => item.score)
            .First()
            .index;

        var scores = Labels
            .Select((label, index) => new KeyValuePair<string, float>(label, probabilities[index]))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        return Task.FromResult(new BrainTumorAnalysis(
            Labels[bestIndex],
            probabilities[bestIndex],
            scores,
            "Completed",
            "onnx-resnet50"));
    }

    public void Dispose()
    {
        _session?.Dispose();
    }

    private InferenceSession GetSession()
    {
        if (_session is not null)
        {
            return _session;
        }

        lock (_sessionLock)
        {
            _session ??= new InferenceSession(_modelPath);
            return _session;
        }
    }

    private static DenseTensor<float> Preprocess(byte[] imageBytes)
    {
        using var image = Image.Load<Rgb24>(imageBytes);
        image.Mutate(operation => operation.Resize(224, 224));

        var tensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });

        for (var y = 0; y < 224; y++)
        {
            for (var x = 0; x < 224; x++)
            {
                var pixel = image[x, y];
                tensor[0, 0, y, x] = Normalize(pixel.R);
                tensor[0, 1, y, x] = Normalize(pixel.G);
                tensor[0, 2, y, x] = Normalize(pixel.B);
            }
        }

        return tensor;
    }

    private static float Normalize(byte value) => value / 127.5f - 1f;

    private static float[] Softmax(float[] logits)
    {
        var max = logits.Max();
        var exps = logits.Select(value => MathF.Exp(value - max)).ToArray();
        var sum = exps.Sum();
        return exps.Select(value => value / sum).ToArray();
    }
}
