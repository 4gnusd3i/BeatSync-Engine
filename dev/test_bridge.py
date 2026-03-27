import io
import json
import sys
import unittest
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[1]
if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))

from beatsync.domain.models import RenderResult, RuntimeStatus, SourceInspection

try:
    from beatsync.bridge import cli
    BRIDGE_IMPORT_ERROR = None
except ModuleNotFoundError as exc:
    cli = None
    BRIDGE_IMPORT_ERROR = exc


class BridgeCliTests(unittest.TestCase):
    def test_probe_runtime_writes_json_to_stdout(self):
        if BRIDGE_IMPORT_ERROR is not None:
            self.skipTest(f"Bridge tests skipped: {BRIDGE_IMPORT_ERROR}")

        status = RuntimeStatus(
            python_status="Portable runtime",
            cuda_status="Portable CUDA",
            ffmpeg_status="Portable FFmpeg",
            ready_threads=2,
            cpu_count=8,
            gpu_available=False,
            gpu_info="CPU only",
            nvenc_available=False,
            supported_processing_modes=("cpu", "prores_proxy"),
            default_processing_mode="cpu",
        )

        stdout = io.StringIO()
        stderr = io.StringIO()
        with (
            mock.patch.object(cli, "probe_runtime_status", return_value=status),
            mock.patch.object(sys, "stdout", stdout),
            mock.patch.object(sys, "stderr", stderr),
        ):
            exit_code = cli.main(["probe-runtime"])

        self.assertEqual(exit_code, 0)
        payload = json.loads(stdout.getvalue())
        self.assertEqual(payload["default_processing_mode"], "cpu")

    def test_inspect_sources_writes_json_to_stdout(self):
        if BRIDGE_IMPORT_ERROR is not None:
            self.skipTest(f"Bridge tests skipped: {BRIDGE_IMPORT_ERROR}")

        inspection = SourceInspection(
            normalized_audio_path=r"C:\track.mp3",
            normalized_video_folder=r"C:\clips",
            audio_state="ok",
            audio_title="track.mp3",
            audio_detail="Ready",
            video_state="ok",
            video_title="clips",
            video_detail="2 clips",
            compatible_clip_count=2,
            compatible_extensions=("mp4", "mkv"),
        )

        stdout = io.StringIO()
        stderr = io.StringIO()
        with (
            mock.patch.object(cli, "inspect_sources", return_value=inspection),
            mock.patch.object(sys, "stdout", stdout),
            mock.patch.object(sys, "stderr", stderr),
        ):
            exit_code = cli.main(["inspect-sources", "--audio-path", "a", "--video-folder", "b"])

        self.assertEqual(exit_code, 0)
        payload = json.loads(stdout.getvalue())
        self.assertEqual(payload["compatible_clip_count"], 2)

    def test_render_reads_request_json(self):
        if BRIDGE_IMPORT_ERROR is not None:
            self.skipTest(f"Bridge tests skipped: {BRIDGE_IMPORT_ERROR}")

        result = RenderResult(
            output_path=r"C:\output.mp4",
            preview_path=r"C:\preview.mp4",
            status_text="ok",
        )

        stdout = io.StringIO()
        stderr = io.StringIO()
        request_json = json.dumps(
            {
                "audio_path": "a",
                "video_folder": "b",
                "generation_mode": "auto",
                "cut_intensity": 4.0,
                "smart_preset": "normal",
                "output_filename": "demo.mp4",
                "direction": "forward",
                "playback_speed": "Normal Speed",
                "timing_offset": 0.0,
                "parallel_workers": 2,
                "processing_mode": "cpu",
                "standard_quality": "balanced",
                "create_prores_delivery_mp4": False,
                "custom_resolution": "default",
                "custom_fps": None,
            }
        )
        with (
            mock.patch.object(cli, "process_render", return_value=result),
            mock.patch.object(sys, "stdout", stdout),
            mock.patch.object(sys, "stderr", stderr),
        ):
            exit_code = cli.main(["render", "--request-json", request_json])

        self.assertEqual(exit_code, 0)
        payload = json.loads(stdout.getvalue())
        self.assertEqual(payload["preview_path"], r"C:\preview.mp4")


if __name__ == "__main__":
    unittest.main()
