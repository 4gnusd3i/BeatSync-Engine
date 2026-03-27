using System.Text.Json.Serialization;

namespace BeatSync.Desktop.Models;

public sealed class RuntimeStatusDto
{
    [JsonPropertyName("python_status")]
    public string PythonStatus { get; set; } = string.Empty;

    [JsonPropertyName("cuda_status")]
    public string CudaStatus { get; set; } = string.Empty;

    [JsonPropertyName("ffmpeg_status")]
    public string FfmpegStatus { get; set; } = string.Empty;

    [JsonPropertyName("ready_threads")]
    public int ReadyThreads { get; set; }

    [JsonPropertyName("cpu_count")]
    public int CpuCount { get; set; }

    [JsonPropertyName("default_parallel_workers")]
    public int DefaultParallelWorkers { get; set; }

    [JsonPropertyName("max_parallel_workers")]
    public int MaxParallelWorkers { get; set; }

    [JsonPropertyName("gpu_available")]
    public bool GpuAvailable { get; set; }

    [JsonPropertyName("gpu_info")]
    public string GpuInfo { get; set; } = string.Empty;

    [JsonPropertyName("nvenc_available")]
    public bool NvencAvailable { get; set; }

    [JsonPropertyName("supported_processing_modes")]
    public string[] SupportedProcessingModes { get; set; } = [];

    [JsonPropertyName("default_processing_mode")]
    public string DefaultProcessingMode { get; set; } = "cpu";
}
