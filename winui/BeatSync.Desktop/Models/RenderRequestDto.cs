using System.Text.Json.Serialization;

namespace BeatSync.Desktop.Models;

public sealed class RenderRequestDto
{
    [JsonPropertyName("audio_path")]
    public string AudioPath { get; set; } = string.Empty;

    [JsonPropertyName("video_folder")]
    public string VideoFolder { get; set; } = string.Empty;

    [JsonPropertyName("generation_mode")]
    public string GenerationMode { get; set; } = "auto";

    [JsonPropertyName("cut_intensity")]
    public double CutIntensity { get; set; } = 4.0;

    [JsonPropertyName("smart_preset")]
    public string SmartPreset { get; set; } = "normal";

    [JsonPropertyName("output_filename")]
    public string OutputFilename { get; set; } = "music_video.mp4";

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "forward";

    [JsonPropertyName("playback_speed")]
    public string PlaybackSpeed { get; set; } = "Normal Speed";

    [JsonPropertyName("timing_offset")]
    public double TimingOffset { get; set; }

    [JsonPropertyName("parallel_workers")]
    public int ParallelWorkers { get; set; } = 4;

    [JsonPropertyName("processing_mode")]
    public string ProcessingMode { get; set; } = "cpu";

    [JsonPropertyName("standard_quality")]
    public string StandardQuality { get; set; } = "balanced";

    [JsonPropertyName("create_prores_delivery_mp4")]
    public bool CreateProresDeliveryMp4 { get; set; }

    [JsonPropertyName("custom_resolution")]
    public string CustomResolution { get; set; } = "default";

    [JsonPropertyName("custom_fps")]
    public double? CustomFps { get; set; }
}
