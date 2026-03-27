using System.Text.Json.Serialization;

namespace BeatSync.Desktop.Models;

public sealed class SourceInspectionDto
{
    [JsonPropertyName("normalized_audio_path")]
    public string? NormalizedAudioPath { get; set; }

    [JsonPropertyName("normalized_video_folder")]
    public string? NormalizedVideoFolder { get; set; }

    [JsonPropertyName("audio_state")]
    public string AudioState { get; set; } = string.Empty;

    [JsonPropertyName("audio_title")]
    public string AudioTitle { get; set; } = string.Empty;

    [JsonPropertyName("audio_detail")]
    public string AudioDetail { get; set; } = string.Empty;

    [JsonPropertyName("video_state")]
    public string VideoState { get; set; } = string.Empty;

    [JsonPropertyName("video_title")]
    public string VideoTitle { get; set; } = string.Empty;

    [JsonPropertyName("video_detail")]
    public string VideoDetail { get; set; } = string.Empty;

    [JsonPropertyName("compatible_clip_count")]
    public int CompatibleClipCount { get; set; }

    [JsonPropertyName("compatible_extensions")]
    public string[] CompatibleExtensions { get; set; } = [];
}
