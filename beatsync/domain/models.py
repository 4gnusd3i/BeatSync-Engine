"""Typed contracts used by the backend, bridge, and UI adapters."""

from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(frozen=True)
class RuntimeStatus:
    python_status: str
    cuda_status: str
    ffmpeg_status: str
    ready_threads: int
    cpu_count: int
    default_parallel_workers: int
    max_parallel_workers: int
    gpu_available: bool
    gpu_info: str
    nvenc_available: bool
    supported_processing_modes: tuple[str, ...]
    default_processing_mode: str


@dataclass(frozen=True)
class SourceInspection:
    normalized_audio_path: str | None
    normalized_video_folder: str | None
    audio_state: str
    audio_title: str
    audio_detail: str
    video_state: str
    video_title: str
    video_detail: str
    compatible_clip_count: int = 0
    compatible_extensions: tuple[str, ...] = field(default_factory=tuple)


@dataclass(frozen=True)
class RenderRequest:
    audio_path: str
    video_folder: str
    generation_mode: str
    cut_intensity: float
    smart_preset: str
    output_filename: str
    direction: str
    playback_speed: str
    timing_offset: float
    parallel_workers: int
    processing_mode: str
    standard_quality: str
    create_prores_delivery_mp4: bool
    custom_resolution: str
    custom_fps: float | None = None


@dataclass(frozen=True)
class RenderResult:
    output_path: str | None
    preview_path: str | None
    status_text: str
    delivery_mp4_path: str | None = None
    effective_target_size: tuple[int, int] | None = None
    effective_fps: float | None = None
    resolved_audio_path: str | None = None
    resolved_video_paths: tuple[str, ...] = field(default_factory=tuple)
    error_text: str | None = None
