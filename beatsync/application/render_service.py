"""Render orchestration service."""

from __future__ import annotations

import datetime
import os
import subprocess

from ..analysis.auto import analyze_beats_auto
from ..analysis.manual import analyze_beats_manual, process_manual_intensity
from ..analysis.smart import analyze_beats_smart, get_gpu_info, is_gpu_available, select_beats_smart, set_gpu_mode
from ..domain.constants import OUTPUT_DIR, REPO_ROOT
from ..domain.models import RenderRequest, RenderResult
from ..processing.ffmpeg import FFMPEG_PATH, create_lossless_delivery_mp4, get_video_fps, get_video_resolution, is_browser_playable_video, normalize_quality_profile
from ..processing.renderer import CPU_COUNT, create_music_video, estimate_threads_per_job
from ..runtime.portable import configure_portable_runtime
from .path_utils import normalize_yes_no_choice, parse_resolution_choice
from .source_service import resolve_inputs
from .status_service import (
    get_success_message_auto,
    get_success_message_manual_skipped,
    get_success_message_manual_subdivided,
    get_success_message_smart,
)

OUTPUT_DIR.mkdir(exist_ok=True)
RUNTIME = configure_portable_runtime(str(REPO_ROOT))


def _create_browser_preview(output_path: str, preview_path: str, cpu_only_mode: bool, nvenc_available: bool) -> str:
    if is_browser_playable_video(output_path):
        print("   Output is already browser-playable; no preview conversion needed.")
        return output_path

    attempts: list[tuple[str, list[str]]] = []
    preferred_hwaccel = ["-hwaccel", "cuda"] if not cpu_only_mode else ["-hwaccel", "auto"]
    cpu_fallback_hwaccels = [preferred_hwaccel]
    if not cpu_only_mode:
        cpu_fallback_hwaccels.append(["-hwaccel", "auto"])

    if nvenc_available:
        attempts.append(
            (
                "NVENC",
                [
                    FFMPEG_PATH,
                    *preferred_hwaccel,
                    "-i",
                    output_path,
                    "-c:v",
                    "h264_nvenc",
                    "-preset",
                    "p5",
                    "-cq",
                    "23",
                    "-pix_fmt",
                    "yuv420p",
                    "-c:a",
                    "aac",
                    "-b:a",
                    "192k",
                    "-y",
                    preview_path,
                ],
            )
        )

    for hwaccel_args in cpu_fallback_hwaccels:
        attempt_name = "CPU (CUDA decode)" if hwaccel_args == ["-hwaccel", "cuda"] else "CPU"
        attempts.append(
            (
                attempt_name,
                [
                    FFMPEG_PATH,
                    *hwaccel_args,
                    "-i",
                    output_path,
                    "-c:v",
                    "libx264",
                    "-preset",
                    "veryfast",
                    "-crf",
                    "23",
                    "-pix_fmt",
                    "yuv420p",
                    "-c:a",
                    "aac",
                    "-b:a",
                    "192k",
                    "-movflags",
                    "+faststart",
                    "-y",
                    preview_path,
                ],
            )
        )

    for encoder_name, preview_cmd in attempts:
        try:
            if os.path.exists(preview_path):
                os.remove(preview_path)
        except OSError:
            pass

        result = subprocess.run(preview_cmd, capture_output=True, text=True, timeout=180)
        if result.returncode == 0 and os.path.exists(preview_path):
            print(f"   Preview created with {encoder_name}.")
            return preview_path

        stderr_tail = (result.stderr or "").strip().splitlines()
        error_line = stderr_tail[-1] if stderr_tail else "unknown FFmpeg error"
        print(f"   Preview creation with {encoder_name} failed: {error_line}")

    print("   Preview generation failed. Returning the original output path instead.")
    return output_path


def process_render(request: RenderRequest) -> RenderResult:
    status_text = ""
    try:
        resolved_audio_path, resolved_video_paths = resolve_inputs(request.audio_path, request.video_folder)

        gpu_runtime_available = is_gpu_available()
        set_gpu_mode(gpu_runtime_available)
        is_prores = request.processing_mode == "prores_proxy"
        create_prores_delivery_mp4 = normalize_yes_no_choice(request.create_prores_delivery_mp4)
        nvenc_available = gpu_runtime_available and request.processing_mode in {"h264_nvenc", "hevc_nvenc"}
        gpu_encoder = request.processing_mode if nvenc_available else "none"
        quality = normalize_quality_profile(request.standard_quality)
        threads_per_job = estimate_threads_per_job(request.parallel_workers)

        if request.generation_mode == "manual":
            smart_mode = False
        elif request.generation_mode == "smart":
            smart_mode = True
        else:
            smart_mode = False

        if is_prores:
            codec_info = "ProRes 422 Proxy (.mov) - Lossless"
            encoder_info = "Lossless Concatenation"
        elif nvenc_available:
            codec_info = f"{gpu_encoder.upper()} (.mp4) | {quality.capitalize()} quality"
            encoder_info = f"{gpu_encoder.upper()} | {quality.capitalize()}"
        else:
            codec_info = f"H.264 (.mp4) | {quality.capitalize()} quality"
            encoder_info = f"libx264 | {quality.capitalize()}"

        output_fps = request.custom_fps if request.custom_fps is not None and request.custom_fps > 0 else get_video_fps(resolved_video_paths[0])
        selected_target_size = parse_resolution_choice(request.custom_resolution)
        resolved_target_size = selected_target_size or get_video_resolution(resolved_video_paths[0])
        resolution_info = (
            f"{resolved_target_size[0]}x{resolved_target_size[1]} (custom)"
            if selected_target_size is not None
            else f"{resolved_target_size[0]}x{resolved_target_size[1]} (auto-detected)"
        )

        name, _ = os.path.splitext(request.output_filename or "music_video.mp4")
        safe_name = os.path.basename(name) or "music_video"
        ext = ".mov" if is_prores else ".mp4"
        timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        filename = f"{safe_name}_{timestamp}{ext}"
        output_path = OUTPUT_DIR / filename

        speed_factor = {"Half Speed": 0.5, "Double Speed": 2.0}.get(request.playback_speed, 1.0)

        if request.generation_mode == "manual":
            beat_times, beat_info = analyze_beats_manual(resolved_audio_path, use_gpu=gpu_runtime_available)
            selected_beats = process_manual_intensity(beat_times, request.cut_intensity)
            intensity_param = request.cut_intensity
        elif request.generation_mode == "smart":
            beat_times, beat_info = analyze_beats_smart(resolved_audio_path)
            selected_beats = select_beats_smart(beat_info, preset=request.smart_preset)
            intensity_param = request.smart_preset
        else:
            selected_beats, beat_info = analyze_beats_auto(resolved_audio_path, use_gpu=gpu_runtime_available)
            intensity_param = "auto"
            beat_times = beat_info.get("times", selected_beats)

        create_music_video(
            resolved_audio_path,
            resolved_video_paths,
            selected_beats,
            intensity_param,
            output_file=str(output_path),
            direction=request.direction,
            speed_factor=speed_factor,
            timing_offset=request.timing_offset,
            max_workers=request.parallel_workers,
            smart_mode=smart_mode,
            beat_info=beat_info,
            lossless_mode=is_prores,
            use_gpu=gpu_runtime_available,
            gpu_encoder=gpu_encoder,
            fps=output_fps,
            target_size=resolved_target_size,
            quality=quality,
            mode_name=request.generation_mode,
        )

        preview_path = str(output_path)
        delivery_mp4_path = None
        preview_source_path = str(output_path)
        cpu_only_mode = not gpu_runtime_available

        if is_prores:
            if create_prores_delivery_mp4:
                delivery_mp4_filename = f"{safe_name}_{timestamp}_delivery_lossless.mp4"
                candidate_path = OUTPUT_DIR / delivery_mp4_filename
                try:
                    preview_source_path = create_lossless_delivery_mp4(
                        str(output_path),
                        str(candidate_path),
                        prefer_cuda_decode=not cpu_only_mode,
                    )
                    delivery_mp4_path = str(candidate_path)
                except Exception as exc:
                    status_text += f"\nDelivery MP4: Failed ({exc})"

            preview_filename = f"{safe_name}_{timestamp}_preview.mp4"
            preview_path = _create_browser_preview(preview_source_path, str(OUTPUT_DIR / preview_filename), cpu_only_mode, nvenc_available)
        elif not is_browser_playable_video(str(output_path)):
            preview_filename = f"{safe_name}_{timestamp}_preview.mp4"
            preview_path = _create_browser_preview(str(output_path), str(OUTPUT_DIR / preview_filename), cpu_only_mode, nvenc_available)

        python_str = "Portable" if RUNTIME.using_portable_python else "System"
        cuda_str = "Portable" if RUNTIME.using_portable_cuda else "System/None"
        gpu_info = f"GPU: {get_gpu_info()}" if gpu_runtime_available else "CPU"
        fps_info = f"{output_fps:.2f} FPS (custom)" if request.custom_fps else f"{output_fps:.2f} FPS (auto-detected)"
        audio_info = "PCM 24-bit (48kHz)" if is_prores else "AAC 320 kbps (48kHz)"

        if request.generation_mode == "smart":
            total_cuts = len(selected_beats) - 1
            status_text = get_success_message_smart(
                request.smart_preset,
                len(beat_times),
                beat_info.get("tempo", 120),
                total_cuts,
                python_str,
                cuda_str,
                threads_per_job,
                CPU_COUNT,
                request.parallel_workers,
                gpu_info,
                encoder_info,
                codec_info,
                fps_info,
                filename,
                audio_info,
            )
        elif request.generation_mode == "auto":
            total_cuts = len(selected_beats) - 1
            sections_info = beat_info.get("selection_info", [])
            status_text = get_success_message_auto(
                total_cuts,
                len(beat_times),
                beat_info.get("tempo", 120),
                sections_info,
                python_str,
                cuda_str,
                threads_per_job,
                CPU_COUNT,
                request.parallel_workers,
                gpu_info,
                encoder_info,
                codec_info,
                fps_info,
                filename,
                audio_info,
            )
        else:
            if request.cut_intensity < 1.0:
                subdivisions = int(1.0 / request.cut_intensity)
                total_cuts = len(selected_beats) - 1
                status_text = get_success_message_manual_subdivided(
                    total_cuts,
                    subdivisions,
                    len(beat_times),
                    beat_info.get("tempo", 120),
                    request.cut_intensity,
                    python_str,
                    cuda_str,
                    threads_per_job,
                    CPU_COUNT,
                    request.parallel_workers,
                    gpu_info,
                    encoder_info,
                    codec_info,
                    fps_info,
                    filename,
                    audio_info,
                )
            else:
                beats_used = len(selected_beats) - 1
                cut_intensity_int = int(request.cut_intensity)
                status_text = get_success_message_manual_skipped(
                    beats_used,
                    cut_intensity_int,
                    len(beat_times),
                    beat_info.get("tempo", 120),
                    request.cut_intensity,
                    python_str,
                    cuda_str,
                    threads_per_job,
                    CPU_COUNT,
                    request.parallel_workers,
                    gpu_info,
                    encoder_info,
                    codec_info,
                    fps_info,
                    filename,
                    audio_info,
                )

        if delivery_mp4_path:
            status_text += f"\nDelivery MP4: {os.path.basename(delivery_mp4_path)}"
        if preview_path and os.path.normcase(preview_path) != os.path.normcase(str(output_path)):
            status_text += f"\nBrowser Preview: {os.path.basename(preview_path)}"
        status_text += f"\nTarget Resolution: {resolution_info}"

        return RenderResult(
            output_path=str(output_path),
            preview_path=preview_path,
            status_text=status_text,
            delivery_mp4_path=delivery_mp4_path,
            effective_target_size=resolved_target_size,
            effective_fps=output_fps,
            resolved_audio_path=resolved_audio_path,
            resolved_video_paths=tuple(resolved_video_paths),
        )
    except Exception as exc:
        return RenderResult(
            output_path=None,
            preview_path=None,
            status_text=f"Error: {exc}",
            error_text=str(exc),
        )
