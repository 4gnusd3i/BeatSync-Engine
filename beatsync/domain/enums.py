"""Enum values shared between Python and WinUI clients."""

from __future__ import annotations

from enum import StrEnum


class GenerationMode(StrEnum):
    MANUAL = "manual"
    SMART = "smart"
    AUTO = "auto"


class ProcessingMode(StrEnum):
    CPU = "cpu"
    H264_NVENC = "h264_nvenc"
    HEVC_NVENC = "hevc_nvenc"
    PRORES_PROXY = "prores_proxy"


class StandardQuality(StrEnum):
    FAST = "fast"
    BALANCED = "balanced"
    HIGH = "high"


class PlaybackDirection(StrEnum):
    FORWARD = "forward"
    BACKWARD = "backward"
    RANDOM = "random"

