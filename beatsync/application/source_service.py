"""Source inspection and input-resolution services."""

from __future__ import annotations

import os
from pathlib import Path

from ..domain.constants import SUPPORTED_AUDIO_EXTENSIONS
from ..domain.models import SourceInspection
from .path_utils import normalize_local_path


def get_video_files(directory: str) -> list[str]:
    from ..processing.renderer import get_video_files as backend_get_video_files

    return backend_get_video_files(directory)


def inspect_sources(audio_path: str, video_folder: str) -> SourceInspection:
    normalized_audio = normalize_local_path(audio_path or "")
    normalized_folder = normalize_local_path(video_folder or "")

    audio_state = "neutral"
    audio_title = "Not selected"
    audio_detail = "Choose the song that BeatSync should analyze."

    if normalized_audio:
        if os.path.isfile(normalized_audio):
            audio_ext = Path(normalized_audio).suffix.lower()
            if audio_ext in SUPPORTED_AUDIO_EXTENSIONS:
                audio_state = "ok"
                audio_title = Path(normalized_audio).name
                audio_detail = f"Ready to analyze as {audio_ext.lstrip('.').upper()} audio."
            else:
                audio_state = "warn"
                audio_title = Path(normalized_audio).name
                audio_detail = "The file exists, but it must be MP3, WAV, or FLAC."
        else:
            audio_state = "warn"
            audio_title = "Path not found"
            audio_detail = "Choose a valid local MP3, WAV, or FLAC file."

    video_state = "neutral"
    video_title = "Not selected"
    video_detail = "Choose the folder that contains your source clips."
    clip_count = 0
    extensions: tuple[str, ...] = ()

    if normalized_folder:
        if os.path.isdir(normalized_folder):
            try:
                video_paths = get_video_files(normalized_folder)
            except Exception as exc:
                video_state = "warn"
                video_title = Path(normalized_folder).name
                video_detail = f"Folder could not be scanned: {exc}"
            else:
                if video_paths:
                    clip_count = len(video_paths)
                    extensions = tuple(sorted({Path(path).suffix.lower().lstrip(".") for path in video_paths}))
                    extension_text = ", ".join(ext.upper() for ext in extensions)
                    video_state = "ok"
                    video_title = Path(normalized_folder).name
                    video_detail = f"{clip_count} compatible clips found ({extension_text})."
                else:
                    video_state = "warn"
                    video_title = Path(normalized_folder).name
                    video_detail = "The folder is valid, but no MP4 or MKV clips were found."
        else:
            video_state = "warn"
            video_title = "Path not found"
            video_detail = "Choose a valid local folder. BeatSync scans subfolders recursively."

    return SourceInspection(
        normalized_audio_path=normalized_audio,
        normalized_video_folder=normalized_folder,
        audio_state=audio_state,
        audio_title=audio_title,
        audio_detail=audio_detail,
        video_state=video_state,
        video_title=video_title,
        video_detail=video_detail,
        compatible_clip_count=clip_count,
        compatible_extensions=extensions,
    )


def resolve_inputs(audio_path: str, video_folder: str) -> tuple[str, list[str]]:
    resolved_audio = normalize_local_path(audio_path)
    if not resolved_audio:
        raise ValueError("Enter a local audio file path.")
    if not os.path.isfile(resolved_audio):
        raise FileNotFoundError(f"Audio file not found: {resolved_audio}")
    if Path(resolved_audio).suffix.lower() not in SUPPORTED_AUDIO_EXTENSIONS:
        raise ValueError("Audio file must be MP3, WAV, or FLAC.")

    resolved_video_folder = normalize_local_path(video_folder)
    if not resolved_video_folder:
        raise ValueError("Enter a local video folder path.")
    if not os.path.isdir(resolved_video_folder):
        raise NotADirectoryError(f"Video folder not found: {resolved_video_folder}")

    resolved_video_paths = get_video_files(resolved_video_folder)
    return resolved_audio, resolved_video_paths
