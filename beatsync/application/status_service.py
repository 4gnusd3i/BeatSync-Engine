"""Human-readable status builders for startup and render results."""

from __future__ import annotations

from ..domain.constants import CONSOLE_SEPARATOR
from ..domain.presets import SMART_PRESETS_CONFIG


def get_ready_status(
    python_status: str,
    cuda_status: str,
    max_threads: int,
    cpu_count: int,
    ffmpeg_status: str,
    gpu_available: bool,
    gpu_info: str,
    nvenc_available: bool,
) -> str:
    gpu_line = f"Analysis: {gpu_info}\n" if gpu_available else "Analysis: CPU only\n"
    nvenc_line = "Video encode: NVENC available\n" if nvenc_available else "Video encode: CPU / ProRes\n"
    return (
        "Ready to render\n\n"
        f"Python: {python_status}\n"
        f"CUDA: {cuda_status}\n"
        f"CPU budget: {max_threads}/{cpu_count} threads per encode\n"
        f"FFmpeg: {ffmpeg_status}\n"
        f"{gpu_line}{nvenc_line}"
        "Modes: Manual | Smart | Auto\n"
        "Delivery paths: H.264 | HEVC | ProRes 422 Proxy\n"
        "Source handling: Local files stay in place\n\n"
        "Choose a local audio track and clip folder to begin."
    )


def get_success_message_smart(
    preset: str,
    total_beats: int,
    tempo: float,
    total_cuts: int,
    python_str: str,
    cuda_str: str,
    max_threads: int,
    cpu_count: int,
    parallel_workers: int,
    gpu_info: str,
    encoder_info: str,
    codec_info: str,
    fps_info: str,
    filename: str,
    audio_info: str,
) -> str:
    preset_info = SMART_PRESETS_CONFIG[preset]
    return f"""Video created successfully!

Smart Mode: {preset.upper()}
   - {preset_info['description']}
   - {total_beats} beats detected at {tempo:.1f} BPM
   - {total_cuts} rhythm-based cuts

Performance:
   - Python: {python_str} | CUDA: {cuda_str}
   - CPU: {max_threads}/{cpu_count} threads | Workers: {parallel_workers}
   - Audio: {gpu_info} | Video: {encoder_info}

Export:
   - {codec_info} | {fps_info} | {audio_info}

Output: {filename}"""


def get_success_message_manual_subdivided(
    total_cuts: int,
    subdivisions: int,
    total_beats: int,
    tempo: float,
    cut_intensity: float,
    python_str: str,
    cuda_str: str,
    max_threads: int,
    cpu_count: int,
    parallel_workers: int,
    gpu_info: str,
    encoder_info: str,
    codec_info: str,
    fps_info: str,
    filename: str,
    audio_info: str,
) -> str:
    return f"""Video created successfully!

Manual Mode: {total_cuts} cuts
   - Subdivided {subdivisions}x from {total_beats} beats
   - {tempo:.1f} BPM | Intensity: {cut_intensity}

Performance:
   - Python: {python_str} | CUDA: {cuda_str}
   - CPU: {max_threads}/{cpu_count} threads | Workers: {parallel_workers}
   - Audio: {gpu_info} | Video: {encoder_info}

Export:
   - {codec_info} | {fps_info} | {audio_info}

Output: {filename}"""


def get_success_message_manual_skipped(
    beats_used: int,
    cut_intensity_int: int,
    total_beats: int,
    tempo: float,
    cut_intensity: float,
    python_str: str,
    cuda_str: str,
    max_threads: int,
    cpu_count: int,
    parallel_workers: int,
    gpu_info: str,
    encoder_info: str,
    codec_info: str,
    fps_info: str,
    filename: str,
    audio_info: str,
) -> str:
    return f"""Video created successfully!

Manual Mode: {beats_used} cuts
   - Every {cut_intensity_int} beats from {total_beats} detected
   - {tempo:.1f} BPM | Intensity: {cut_intensity}

Performance:
   - Python: {python_str} | CUDA: {cuda_str}
   - CPU: {max_threads}/{cpu_count} threads | Workers: {parallel_workers}
   - Audio: {gpu_info} | Video: {encoder_info}

Export:
   - {codec_info} | {fps_info} | {audio_info}

Output: {filename}"""


def get_success_message_auto(
    total_cuts: int,
    total_beats: int,
    tempo: float,
    sections_info: list[dict],
    python_str: str,
    cuda_str: str,
    max_threads: int,
    cpu_count: int,
    parallel_workers: int,
    gpu_info: str,
    encoder_info: str,
    codec_info: str,
    fps_info: str,
    filename: str,
    audio_info: str,
) -> str:
    section_summary = ""
    if sections_info:
        section_summary = "\n   - Sections analyzed:\n"
        for section in sections_info:
            section_summary += (
                f"      * {section['section'].capitalize()}: "
                f"{section['selected_beats']}/{section['total_beats']} beats "
                f"({section['selection_ratio'] * 100:.1f}%)\n"
            )

    return f"""Video created successfully!

Auto Mode: {total_cuts} cuts
   - {total_beats} beats detected at {tempo:.1f} BPM
   - Automatic song structure analysis
   - Adaptive cut frequency per section{section_summary}Performance:
   - Python: {python_str} | CUDA: {cuda_str}
   - CPU: {max_threads}/{cpu_count} threads | Workers: {parallel_workers}
   - Audio: {gpu_info} | Video: {encoder_info}

Export:
   - {codec_info} | {fps_info} | {audio_info}

Output: {filename}"""


def get_startup_header(
    cpu_count: int,
    max_threads: int,
    parallel_workers: int,
    python_status: str,
    cuda_status: str,
    librosa_version: str,
    ffmpeg_status: str,
    gpu_available: bool,
    gpu_info: str,
    nvenc_available: bool,
) -> str:
    gpu_line = f"   GPU: {gpu_info} (Auto-enabled)" if gpu_available else "   GPU: Not available (CPU only)"
    nvenc_line = "   NVENC: Available (Auto-enabled)" if nvenc_available else "   NVENC: Not available"
    return f"""{CONSOLE_SEPARATOR}
BeatSync Engine
{CONSOLE_SEPARATOR}
   Python: {python_status}
   CUDA: {cuda_status}
   FFmpeg: {ffmpeg_status}
   Librosa: {librosa_version}
   CPU: {cpu_count} threads ({max_threads} max per encode)
   Parallel Workers: {parallel_workers}
   {gpu_line}
   {nvenc_line}
   Modes: Manual | Smart | Auto
   ProRes 422 Proxy: ENABLED"""

