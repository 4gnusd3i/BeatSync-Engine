"""Shared constants used across the backend and UI adapters."""

from __future__ import annotations

from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
OUTPUT_DIR = REPO_ROOT / "output"
TEMP_DIR = REPO_ROOT / "temp"

SUPPORTED_AUDIO_EXTENSIONS = {".mp3", ".wav", ".flac"}
DEFAULT_GUI_PORT = 7860
CONSOLE_SEPARATOR = "=" * 70

