using System.Text.Json.Serialization;

namespace BeatSync.Desktop.Models;

public sealed class RenderResultDto
{
    [JsonPropertyName("output_path")]
    public string? OutputPath { get; set; }

    [JsonPropertyName("preview_path")]
    public string? PreviewPath { get; set; }

    [JsonPropertyName("status_text")]
    public string StatusText { get; set; } = string.Empty;

    [JsonPropertyName("delivery_mp4_path")]
    public string? DeliveryMp4Path { get; set; }

    [JsonPropertyName("effective_target_size")]
    public int[]? EffectiveTargetSize { get; set; }

    [JsonPropertyName("effective_fps")]
    public double? EffectiveFps { get; set; }

    [JsonPropertyName("resolved_audio_path")]
    public string? ResolvedAudioPath { get; set; }

    [JsonPropertyName("resolved_video_paths")]
    public string[] ResolvedVideoPaths { get; set; } = [];

    [JsonPropertyName("error_text")]
    public string? ErrorText { get; set; }
}
