using System.Globalization;
using System.Text;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using BeatSync.Desktop.Models;
using BeatSync.Desktop.Services;

namespace BeatSync.Desktop.ViewModels;

public sealed class MainViewModel : BindableBase
{
    private static readonly IReadOnlyList<OptionItem> FallbackProcessingModes =
    [
        new("CPU H.264", "cpu"),
        new("ProRes 422 Proxy", "prores_proxy"),
    ];

    private readonly DispatcherQueue? _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly StringBuilder _debugLogBuilder = new();
    private PythonBridgeService? _bridge;
    private CancellationTokenSource? _inspectionCts;
    private bool _runtimeInitialized;
    private bool _isBusy = true;
    private string _busyStateText = "Initializing";
    private string _audioPath = string.Empty;
    private string _videoFolder = string.Empty;
    private string _generationMode = "auto";
    private string _smartPreset = "normal";
    private double _cutIntensity = 4.0;
    private string _direction = "forward";
    private string _playbackSpeed = "Normal Speed";
    private double _timingOffset;
    private string _processingMode = "cpu";
    private string _standardQuality = "balanced";
    private bool _createProresDeliveryMp4;
    private string _customResolution = "default";
    private string _customFpsText = string.Empty;
    private double _parallelWorkers = 4.0;
    private double _maximumParallelWorkers = 4.0;
    private string _outputFilename = "music_video.mp4";
    private string _runtimeSummaryText = "Probing the portable Python runtime...";
    private string _sourceSummaryText = "Choose the song and clip folder for this render.";
    private string _statusText = "Starting BeatSync Desktop...";
    private string _debugLogText = "Backend debug output will appear here.";
    private string? _previewPath;
    private string _previewPlaceholderText = "After a render, the latest browser-playable preview appears here.";
    private IReadOnlyList<OptionItem> _processingModes = FallbackProcessingModes;

    public MainViewModel()
    {
        RenderCommand = new AsyncCommand(RenderAsync, () => !IsBusy);
    }

    public IReadOnlyList<OptionItem> GenerationModes { get; } =
    [
        new("Auto (recommended)", "auto"),
        new("Smart", "smart"),
        new("Manual", "manual"),
    ];

    public IReadOnlyList<OptionItem> SmartPresets { get; } =
    [
        new("Slower", "slower"),
        new("Slow", "slow"),
        new("Normal", "normal"),
        new("Fast", "fast"),
        new("Faster", "faster"),
    ];

    public IReadOnlyList<OptionItem> Directions { get; } =
    [
        new("Forward", "forward"),
        new("Backward", "backward"),
        new("Random", "random"),
    ];

    public IReadOnlyList<OptionItem> PlaybackSpeeds { get; } =
    [
        new("Normal speed", "Normal Speed"),
        new("Half speed", "Half Speed"),
        new("Double speed", "Double Speed"),
    ];

    public IReadOnlyList<OptionItem> StandardQualities { get; } =
    [
        new("Fast", "fast"),
        new("Balanced", "balanced"),
        new("High", "high"),
    ];

    public IReadOnlyList<OptionItem> ResolutionPresets { get; } =
    [
        new("Default (match first source video)", "default"),
        new("16:9 | 1280x720 (HD)", "1280x720"),
        new("16:9 | 1920x1080 (Full HD)", "1920x1080"),
        new("16:9 | 2560x1440 (QHD)", "2560x1440"),
        new("16:9 | 3840x2160 (4K UHD)", "3840x2160"),
        new("21:9 | 2560x1080 (UltraWide HD)", "2560x1080"),
        new("21:9 | 3440x1440 (UltraWide QHD)", "3440x1440"),
        new("21:9 | 3840x1600 (UltraWide 1600p)", "3840x1600"),
        new("9:16 | 720x1280 (Vertical HD)", "720x1280"),
        new("9:16 | 1080x1920 (Vertical Full HD)", "1080x1920"),
        new("9:16 | 1440x2560 (Vertical QHD)", "1440x2560"),
    ];

    public AsyncCommand RenderCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(RenderButtonText));
                RenderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string BusyStateText
    {
        get => _busyStateText;
        private set => SetProperty(ref _busyStateText, value);
    }

    public string AudioPath
    {
        get => _audioPath;
        set
        {
            if (SetProperty(ref _audioPath, value))
            {
                ScheduleSourceInspection();
            }
        }
    }

    public string VideoFolder
    {
        get => _videoFolder;
        set
        {
            if (SetProperty(ref _videoFolder, value))
            {
                ScheduleSourceInspection();
            }
        }
    }

    public string GenerationMode
    {
        get => _generationMode;
        set
        {
            if (SetProperty(ref _generationMode, value))
            {
                OnPropertyChanged(nameof(SmartPresetVisibility));
                OnPropertyChanged(nameof(ManualIntensityVisibility));
            }
        }
    }

    public string SmartPreset
    {
        get => _smartPreset;
        set => SetProperty(ref _smartPreset, value);
    }

    public double CutIntensity
    {
        get => _cutIntensity;
        set
        {
            if (SetProperty(ref _cutIntensity, value))
            {
                OnPropertyChanged(nameof(CutIntensityCaption));
            }
        }
    }

    public string Direction
    {
        get => _direction;
        set => SetProperty(ref _direction, value);
    }

    public string PlaybackSpeed
    {
        get => _playbackSpeed;
        set => SetProperty(ref _playbackSpeed, value);
    }

    public double TimingOffset
    {
        get => _timingOffset;
        set => SetProperty(ref _timingOffset, value);
    }

    public IReadOnlyList<OptionItem> ProcessingModes
    {
        get => _processingModes;
        private set => SetProperty(ref _processingModes, value);
    }

    public string ProcessingMode
    {
        get => _processingMode;
        set
        {
            if (SetProperty(ref _processingMode, value))
            {
                OnPropertyChanged(nameof(StandardQualityVisibility));
                OnPropertyChanged(nameof(ProresDeliveryVisibility));
                _ = UpdateRecommendedOutputFilenameAsync();
            }
        }
    }

    public string StandardQuality
    {
        get => _standardQuality;
        set => SetProperty(ref _standardQuality, value);
    }

    public bool CreateProresDeliveryMp4
    {
        get => _createProresDeliveryMp4;
        set => SetProperty(ref _createProresDeliveryMp4, value);
    }

    public string CustomResolution
    {
        get => _customResolution;
        set => SetProperty(ref _customResolution, value);
    }

    public string CustomFpsText
    {
        get => _customFpsText;
        set => SetProperty(ref _customFpsText, value);
    }

    public double ParallelWorkers
    {
        get => _parallelWorkers;
        set
        {
            var clamped = Math.Clamp(Math.Round(value), 1, MaximumParallelWorkers);
            if (SetProperty(ref _parallelWorkers, clamped))
            {
                OnPropertyChanged(nameof(ParallelWorkersCaption));
            }
        }
    }

    public double MaximumParallelWorkers
    {
        get => _maximumParallelWorkers;
        private set
        {
            var maxValue = Math.Max(1, value);
            if (SetProperty(ref _maximumParallelWorkers, maxValue))
            {
                if (ParallelWorkers > maxValue)
                {
                    ParallelWorkers = maxValue;
                }

                OnPropertyChanged(nameof(ParallelWorkersCaption));
            }
        }
    }

    public string OutputFilename
    {
        get => _outputFilename;
        set => SetProperty(ref _outputFilename, value);
    }

    public string RuntimeSummaryText
    {
        get => _runtimeSummaryText;
        private set => SetProperty(ref _runtimeSummaryText, value);
    }

    public string SourceSummaryText
    {
        get => _sourceSummaryText;
        private set => SetProperty(ref _sourceSummaryText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string DebugLogText
    {
        get => _debugLogText;
        private set => SetProperty(ref _debugLogText, value);
    }

    public string? PreviewPath
    {
        get => _previewPath;
        private set
        {
            if (SetProperty(ref _previewPath, value))
            {
                OnPropertyChanged(nameof(PreviewPlayerVisibility));
                OnPropertyChanged(nameof(PreviewPlaceholderVisibility));
            }
        }
    }

    public string PreviewPlaceholderText
    {
        get => _previewPlaceholderText;
        private set => SetProperty(ref _previewPlaceholderText, value);
    }

    public string RenderButtonText => IsBusy ? "Creating..." : "Create Music Video";
    public string ParallelWorkersCaption => $"{(int)ParallelWorkers} concurrent jobs, up to {Math.Max(1, (int)MaximumParallelWorkers)} on this machine";
    public string CutIntensityCaption => $"Intensity: {CutIntensity.ToString("0.0", CultureInfo.InvariantCulture)}";
    public Visibility SmartPresetVisibility => GenerationMode == "smart" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ManualIntensityVisibility => GenerationMode == "manual" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility StandardQualityVisibility => ProcessingMode == "prores_proxy" ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ProresDeliveryVisibility => ProcessingMode == "prores_proxy" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PreviewPlayerVisibility => string.IsNullOrWhiteSpace(PreviewPath) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility PreviewPlaceholderVisibility => string.IsNullOrWhiteSpace(PreviewPath) ? Visibility.Visible : Visibility.Collapsed;

    public async Task InitializeAsync()
    {
        if (_runtimeInitialized)
        {
            return;
        }

        try
        {
            _bridge = new PythonBridgeService(RepoLocator.LocateRepoRoot());
            ResetDebugLog("Runtime probe output will appear here.");
            var runtimeCommand = await _bridge.ProbeRuntimeAsync(onStandardErrorLine: AppendDebugLogLine);
            var runtime = runtimeCommand.Payload;
            RuntimeSummaryText = FormatRuntimeSummary(runtime);
            MaximumParallelWorkers = runtime.MaxParallelWorkers;
            ParallelWorkers = runtime.DefaultParallelWorkers;
            ProcessingModes = BuildProcessingModes(runtime.SupportedProcessingModes);
            ProcessingMode = runtime.DefaultProcessingMode;
            StatusText = FormatReadyStatus(runtime);
            EnsureDebugLogFallback("Portable runtime initialized with no backend debug output.");
            await UpdateRecommendedOutputFilenameAsync();
            _runtimeInitialized = true;
            await RefreshSourceInspectionAsync();
        }
        catch (Exception ex)
        {
            RuntimeSummaryText = "Portable runtime startup failed.";
            SourceSummaryText = "Source inspection is unavailable until the backend can be reached.";
            StatusText = $"Startup failed: {ex.Message}";
            PreviewPlaceholderText = "The backend is unavailable, so no preview can be loaded.";
            if (ex is BridgeCommandException bridgeException)
            {
                SetDebugLog(bridgeException.StandardError, "The backend failed before it produced any debug output.");
            }
        }
        finally
        {
            SetBusyState(false, "Ready");
        }
    }

    public async Task RenderAsync()
    {
        if (_bridge is null)
        {
            StatusText = "Render unavailable: the portable Python bridge is not ready.";
            return;
        }

        if (!TryParseCustomFps(out var customFps, out var fpsError))
        {
            StatusText = fpsError;
            return;
        }

        SetBusyState(true, "Rendering");
        StatusText = "Creating...";
        ResetDebugLog("Backend render output will appear here.");

        try
        {
            var request = new RenderRequestDto
            {
                AudioPath = AudioPath,
                VideoFolder = VideoFolder,
                GenerationMode = GenerationMode,
                CutIntensity = CutIntensity,
                SmartPreset = SmartPreset,
                OutputFilename = OutputFilename,
                Direction = Direction,
                PlaybackSpeed = PlaybackSpeed,
                TimingOffset = TimingOffset,
                ParallelWorkers = Math.Max(1, (int)ParallelWorkers),
                ProcessingMode = ProcessingMode,
                StandardQuality = StandardQuality,
                CreateProresDeliveryMp4 = CreateProresDeliveryMp4,
                CustomResolution = CustomResolution,
                CustomFps = customFps,
            };

            var result = await _bridge.RenderAsync(request, onStandardErrorLine: AppendDebugLogLine);
            StatusText = result.Payload.StatusText;
            EnsureDebugLogFallback("The render completed with no backend debug output.");

            var previewPath = FirstExistingPath(result.Payload.PreviewPath, result.Payload.OutputPath);
            PreviewPath = previewPath;
            PreviewPlaceholderText = previewPath is null
                ? "The backend completed, but no playable preview file was returned."
                : $"Loaded preview from {Path.GetFileName(previewPath)}";

            await RefreshSourceInspectionAsync();
        }
        catch (BridgeCommandException ex)
        {
            PreviewPath = null;
            PreviewPlaceholderText = "The render failed before a preview could be loaded.";
            StatusText = $"Render failed: {ex.Message}";
            SetDebugLog(ex.StandardError, "The backend failed before it produced any debug output.");
        }
        catch (Exception ex)
        {
            PreviewPath = null;
            PreviewPlaceholderText = "The render failed before a preview could be loaded.";
            StatusText = $"Render failed: {ex.Message}";
        }
        finally
        {
            SetBusyState(false, "Ready");
        }
    }

    private void SetBusyState(bool isBusy, string stateText)
    {
        IsBusy = isBusy;
        BusyStateText = stateText;
    }

    private void ScheduleSourceInspection()
    {
        if (!_runtimeInitialized || _bridge is null || IsBusy)
        {
            return;
        }

        _inspectionCts?.Cancel();
        _inspectionCts?.Dispose();
        _inspectionCts = new CancellationTokenSource();
        _ = RefreshSourceInspectionAfterDelayAsync(_inspectionCts.Token);
    }

    private async Task RefreshSourceInspectionAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            await RefreshSourceInspectionAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RefreshSourceInspectionAsync(CancellationToken cancellationToken = default)
    {
        if (_bridge is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(AudioPath) && string.IsNullOrWhiteSpace(VideoFolder))
        {
            SourceSummaryText = "Choose the song and clip folder for this render.";
            return;
        }

        try
        {
            var inspection = await _bridge.InspectSourcesAsync(AudioPath, VideoFolder, cancellationToken: cancellationToken);
            SourceSummaryText = FormatSourceSummary(inspection.Payload);
        }
        catch (Exception ex)
        {
            SourceSummaryText = $"Source inspection failed.{Environment.NewLine}{ex.Message}";
        }
    }

    private async Task UpdateRecommendedOutputFilenameAsync()
    {
        if (_bridge is null || string.IsNullOrWhiteSpace(ProcessingMode))
        {
            OutputFilename = ApplyLocalFilenameFallback(OutputFilename, ProcessingMode);
            return;
        }

        try
        {
            var recommended = await _bridge.RecommendOutputFilenameAsync(OutputFilename, ProcessingMode);
            if (!string.IsNullOrWhiteSpace(recommended))
            {
                OutputFilename = recommended;
            }
        }
        catch
        {
            OutputFilename = ApplyLocalFilenameFallback(OutputFilename, ProcessingMode);
        }
    }

    private bool TryParseCustomFps(out double? customFps, out string errorText)
    {
        customFps = null;
        errorText = string.Empty;

        if (string.IsNullOrWhiteSpace(CustomFpsText))
        {
            return true;
        }

        if (!double.TryParse(CustomFpsText, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpsValue))
        {
            errorText = "Custom FPS must be blank or a positive number, for example 23.976 or 60.";
            return false;
        }

        if (fpsValue <= 0)
        {
            errorText = "Custom FPS must be greater than zero.";
            return false;
        }

        customFps = fpsValue;
        return true;
    }

    private static IReadOnlyList<OptionItem> BuildProcessingModes(IEnumerable<string> modes)
    {
        var options = modes.Select(mode => new OptionItem(GetProcessingModeLabel(mode), mode)).ToArray();
        return options.Length == 0 ? FallbackProcessingModes : options;
    }

    private static string FormatRuntimeSummary(RuntimeStatusDto runtime)
    {
        var gpuLine = runtime.GpuAvailable ? runtime.GpuInfo : "CPU only";
        var modeLine = string.Join(", ", runtime.SupportedProcessingModes.Select(GetProcessingModeLabel));

        return string.Join(
            Environment.NewLine,
            $"Python: {runtime.PythonStatus}",
            $"CUDA: {runtime.CudaStatus}",
            $"FFmpeg: {runtime.FfmpegStatus}",
            $"CPU threads: {runtime.CpuCount}",
            $"Default workers: {runtime.DefaultParallelWorkers}",
            $"Threads per job: {runtime.ReadyThreads}",
            $"GPU: {gpuLine}",
            $"Pipelines: {modeLine}"
        );
    }

    private static string FormatReadyStatus(RuntimeStatusDto runtime)
    {
        return string.Join(
            Environment.NewLine,
            "Ready to render.",
            $"Python runtime: {runtime.PythonStatus}",
            $"CUDA runtime: {runtime.CudaStatus}",
            $"FFmpeg: {runtime.FfmpegStatus}",
            $"Parallel workers: {runtime.DefaultParallelWorkers}",
            $"Threads per job: {runtime.ReadyThreads}",
            runtime.GpuAvailable ? $"GPU: {runtime.GpuInfo}" : "GPU: not available",
            runtime.NvencAvailable ? "NVENC: available" : "NVENC: unavailable"
        );
    }

    private static string FormatSourceSummary(SourceInspectionDto inspection)
    {
        return string.Join(
            Environment.NewLine,
            $"Audio: {inspection.AudioTitle}",
            inspection.AudioDetail,
            string.Empty,
            $"Video: {inspection.VideoTitle}",
            inspection.VideoDetail
        );
    }

    private static string? FirstExistingPath(params string?[] paths)
    {
        foreach (var path in paths)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static string ApplyLocalFilenameFallback(string currentFilename, string processingMode)
    {
        var rawName = string.IsNullOrWhiteSpace(currentFilename) ? "music_video.mp4" : currentFilename.Trim();
        var baseName = Path.GetFileNameWithoutExtension(rawName);
        var extension = Path.GetExtension(rawName);
        var targetExtension = processingMode == "prores_proxy" ? ".mov" : ".mp4";

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "music_video";
        }

        return string.IsNullOrWhiteSpace(extension) || extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) || extension.Equals(".mov", StringComparison.OrdinalIgnoreCase)
            ? $"{baseName}{targetExtension}"
            : rawName;
    }

    private static string GetProcessingModeLabel(string mode)
    {
        return mode switch
        {
            "h264_nvenc" => "NVIDIA NVENC H.264",
            "hevc_nvenc" => "NVIDIA NVENC HEVC (H.265)",
            "cpu" => "CPU H.264",
            "prores_proxy" => "ProRes 422 Proxy",
            _ => mode,
        };
    }

    private void ResetDebugLog(string placeholder)
    {
        RunOnUiThread(() =>
        {
            _debugLogBuilder.Clear();
            DebugLogText = placeholder;
        });
    }

    private void EnsureDebugLogFallback(string fallback)
    {
        RunOnUiThread(() =>
        {
            if (_debugLogBuilder.Length == 0)
            {
                DebugLogText = fallback;
            }
        });
    }

    private void SetDebugLog(string? logText, string fallback)
    {
        RunOnUiThread(() =>
        {
            _debugLogBuilder.Clear();

            if (!string.IsNullOrWhiteSpace(logText))
            {
                _debugLogBuilder.Append(logText.Trim());
                DebugLogText = _debugLogBuilder.ToString();
                return;
            }

            DebugLogText = fallback;
        });
    }

    private void AppendDebugLogLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        RunOnUiThread(() =>
        {
            if (_debugLogBuilder.Length > 0)
            {
                _debugLogBuilder.AppendLine();
            }

            _debugLogBuilder.Append(line);
            DebugLogText = _debugLogBuilder.ToString();
        });
    }

    private void RunOnUiThread(Action action)
    {
        if (_dispatcherQueue is not null && !_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(() => action());
            return;
        }

        action();
    }
}
