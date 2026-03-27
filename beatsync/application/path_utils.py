"""Path and value normalization helpers shared across clients."""

from __future__ import annotations

import os
import re
from urllib.parse import unquote, urlparse


def normalize_local_path(path_value: str) -> str | None:
    if not path_value:
        return None

    normalized = path_value.strip().strip('"').strip("'")
    if not normalized:
        return None

    normalized = unquote(normalized)
    parsed = urlparse(normalized)
    if parsed.scheme and parsed.scheme.lower() == "file":
        if parsed.netloc and parsed.path:
            normalized = f"//{parsed.netloc}{parsed.path}"
        else:
            normalized = parsed.path or normalized

    normalized = os.path.expandvars(os.path.expanduser(normalized))
    normalized = normalized.replace("/", os.sep)

    if re.match(r"^[\\/]+[A-Za-z]:[\\/]", normalized):
        normalized = normalized.lstrip("\\/")

    drive_matches = list(re.finditer(r"[A-Za-z]:[\\/]", normalized))
    if len(drive_matches) > 1:
        normalized = normalized[drive_matches[-1].start():]

    if os.path.isabs(normalized):
        return os.path.normpath(normalized)

    return os.path.abspath(normalized)


def parse_resolution_choice(resolution_choice: str | None) -> tuple[int, int] | None:
    normalized = (resolution_choice or "default").strip().lower()
    if not normalized or normalized == "default":
        return None

    width_str, height_str = normalized.split("x", 1)
    width = int(width_str)
    height = int(height_str)
    if width <= 0 or height <= 0:
        raise ValueError("Custom resolution must use positive width and height values.")
    return width, height


def normalize_yes_no_choice(value: bool | str | None) -> bool:
    if isinstance(value, bool):
        return value
    return str(value or "").strip().lower() in {"yes", "true", "on", "1"}


def recommend_output_filename(current_filename: str | None, processing_mode: str) -> str:
    raw_name = (current_filename or "").strip() or "music_video.mp4"
    base_name, ext = os.path.splitext(raw_name)
    normalized_ext = ext.lower()
    target_ext = ".mov" if processing_mode == "prores_proxy" else ".mp4"

    if not base_name:
        base_name = "music_video"

    if normalized_ext in {"", ".mp4", ".mov"}:
        return f"{base_name}{target_ext}"

    return raw_name

