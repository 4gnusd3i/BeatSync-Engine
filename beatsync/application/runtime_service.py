"""Runtime capability probing."""

from __future__ import annotations

import librosa

from ..analysis.smart import get_gpu_info, is_gpu_available
from ..domain.constants import REPO_ROOT
from ..domain.models import RuntimeStatus
from ..processing.ffmpeg import FFMPEG_FOUND, check_nvenc_support
from ..processing.renderer import CPU_COUNT, PARALLEL_WORKERS, estimate_threads_per_job
from ..runtime.portable import configure_portable_runtime
from .status_service import get_startup_header


def probe_runtime_status() -> RuntimeStatus:
    runtime = configure_portable_runtime(str(REPO_ROOT))
    gpu_available = is_gpu_available()
    nvenc_available = gpu_available and check_nvenc_support()
    ready_threads = estimate_threads_per_job(PARALLEL_WORKERS)
    supported_processing_modes = (
        ("h264_nvenc", "hevc_nvenc", "cpu", "prores_proxy")
        if nvenc_available
        else ("cpu", "prores_proxy")
    )
    default_processing_mode = "h264_nvenc" if nvenc_available else "cpu"
    ffmpeg_status = "Portable FFmpeg" if FFMPEG_FOUND else "System FFmpeg"
    max_parallel_workers = min(16, max(CPU_COUNT // 2, 4))

    return RuntimeStatus(
        python_status="Portable runtime" if runtime.using_portable_python else "System Python",
        cuda_status=runtime.cuda_runtime_label if runtime.using_portable_cuda else "System CUDA / not available",
        ffmpeg_status=ffmpeg_status,
        ready_threads=ready_threads,
        cpu_count=CPU_COUNT,
        default_parallel_workers=min(PARALLEL_WORKERS, max_parallel_workers),
        max_parallel_workers=max_parallel_workers,
        gpu_available=gpu_available,
        gpu_info=get_gpu_info(),
        nvenc_available=nvenc_available,
        supported_processing_modes=supported_processing_modes,
        default_processing_mode=default_processing_mode,
    )


def startup_banner() -> str:
    runtime = configure_portable_runtime(str(REPO_ROOT))
    status = probe_runtime_status()
    return get_startup_header(
        status.cpu_count,
        status.ready_threads,
        PARALLEL_WORKERS,
        runtime.python_runtime_label if runtime.using_portable_python else "System Python",
        runtime.cuda_runtime_label,
        librosa.__version__,
        "Portable (bin/ffmpeg/ffmpeg.exe)" if FFMPEG_FOUND else "System FFmpeg (portable not found)",
        status.gpu_available,
        status.gpu_info,
        status.nvenc_available,
    )
