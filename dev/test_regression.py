import datetime
import importlib
import sys
import unittest
import warnings
from pathlib import Path
from unittest import mock

warnings.simplefilter("ignore", ResourceWarning)


REPO_ROOT = Path(__file__).resolve().parents[1]
if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))

from beatsync.application.path_utils import recommend_output_filename
from beatsync.application.source_service import inspect_sources
import beatsync.application.source_service as source_service

try:
    from beatsync.application.render_service import RenderRequest, process_render
    import beatsync.application.render_service as render_service
    BACKEND_IMPORT_ERROR = None
except ModuleNotFoundError as exc:
    RenderRequest = None
    process_render = None
    render_service = None
    BACKEND_IMPORT_ERROR = exc


class FixedDateTime(datetime.datetime):
    @classmethod
    def now(cls, tz=None):
        return cls(2026, 3, 27, 12, 0, 0, tzinfo=tz)


class BackendRegressionTests(unittest.TestCase):
    def test_recommend_output_filename_switches_extension(self):
        self.assertEqual(recommend_output_filename("demo.mp4", "prores_proxy"), "demo.mov")
        self.assertEqual(recommend_output_filename("demo.mov", "cpu"), "demo.mp4")

    def test_source_inspection_reports_valid_local_media(self):
        audio_path = r"C:\media\track.mp3"
        clip_dir = r"C:\media\clips"
        with (
            mock.patch.object(source_service, "normalize_local_path", side_effect=lambda value: value),
            mock.patch.object(source_service.os.path, "isfile", side_effect=lambda value: value == audio_path),
            mock.patch.object(source_service.os.path, "isdir", side_effect=lambda value: value == clip_dir),
            mock.patch.object(source_service, "get_video_files", return_value=[r"C:\media\clips\a.mp4", r"C:\media\clips\b.mkv"]),
        ):
            inspection = inspect_sources(str(audio_path), str(clip_dir))

        self.assertEqual(inspection.audio_state, "ok")
        self.assertEqual(inspection.video_state, "ok")
        self.assertEqual(inspection.compatible_clip_count, 2)
        self.assertIn("Ready to analyze as MP3 audio.", inspection.audio_detail)
        self.assertIn("2 compatible clips found", inspection.video_detail)

    def test_process_render_standard_delivery_path(self):
        if BACKEND_IMPORT_ERROR is not None:
            self.skipTest(f"Backend render tests skipped: {BACKEND_IMPORT_ERROR}")

        with (
            mock.patch.object(render_service, "resolve_inputs", return_value=("C:\\audio.mp3", ["C:\\clips\\a.mp4"])),
            mock.patch.object(render_service, "is_gpu_available", return_value=False),
            mock.patch.object(render_service, "set_gpu_mode"),
            mock.patch.object(
                render_service,
                "analyze_beats_auto",
                return_value=([0.0, 1.0, 2.0], {"tempo": 120.0, "times": [0.0, 1.0, 2.0], "selection_info": []}),
            ),
            mock.patch.object(render_service, "create_music_video") as create_music_video,
            mock.patch.object(render_service, "get_video_fps", return_value=30.0),
            mock.patch.object(render_service, "get_video_resolution", return_value=(1920, 1080)),
            mock.patch.object(render_service, "is_browser_playable_video", return_value=True),
            mock.patch.object(render_service, "get_gpu_info", return_value="GPU Ready"),
            mock.patch.object(render_service.datetime, "datetime", FixedDateTime),
        ):
            result = process_render(
                RenderRequest(
                    audio_path="C:\\audio.mp3",
                    video_folder="C:\\clips",
                    generation_mode="auto",
                    cut_intensity=4.0,
                    smart_preset="normal",
                    output_filename="deliverable.mp4",
                    direction="forward",
                    playback_speed="Normal Speed",
                    timing_offset=0.0,
                    parallel_workers=2,
                    processing_mode="cpu",
                    standard_quality="balanced",
                    create_prores_delivery_mp4=False,
                    custom_resolution="default",
                    custom_fps=None,
                )
            )

        self.assertTrue(result.preview_path.endswith(".mp4"))
        self.assertIn("Video created successfully!", result.status_text)
        self.assertIn("Target Resolution: 1920x1080 (auto-detected)", result.status_text)
        self.assertEqual(result.resolved_audio_path, "C:\\audio.mp3")
        create_music_video.assert_called_once()

    def test_process_render_prores_delivery_copy_path(self):
        if BACKEND_IMPORT_ERROR is not None:
            self.skipTest(f"Backend render tests skipped: {BACKEND_IMPORT_ERROR}")

        with (
            mock.patch.object(render_service, "resolve_inputs", return_value=("C:\\audio.mp3", ["C:\\clips\\a.mp4"])),
            mock.patch.object(render_service, "is_gpu_available", return_value=False),
            mock.patch.object(render_service, "set_gpu_mode"),
            mock.patch.object(
                render_service,
                "analyze_beats_auto",
                return_value=([0.0, 1.0, 2.0], {"tempo": 120.0, "times": [0.0, 1.0, 2.0], "selection_info": []}),
            ),
            mock.patch.object(render_service, "create_music_video"),
            mock.patch.object(render_service, "create_lossless_delivery_mp4", return_value="C:\\output\\deliverable_delivery_lossless.mp4"),
            mock.patch.object(render_service, "_create_browser_preview", return_value="C:\\output\\deliverable_preview.mp4"),
            mock.patch.object(render_service, "get_video_fps", return_value=24.0),
            mock.patch.object(render_service, "get_video_resolution", return_value=(1280, 720)),
            mock.patch.object(render_service, "get_gpu_info", return_value="GPU Ready"),
            mock.patch.object(render_service.datetime, "datetime", FixedDateTime),
        ):
            result = process_render(
                RenderRequest(
                    audio_path="C:\\audio.mp3",
                    video_folder="C:\\clips",
                    generation_mode="auto",
                    cut_intensity=4.0,
                    smart_preset="normal",
                    output_filename="master.mov",
                    direction="forward",
                    playback_speed="Normal Speed",
                    timing_offset=0.0,
                    parallel_workers=2,
                    processing_mode="prores_proxy",
                    standard_quality="balanced",
                    create_prores_delivery_mp4=True,
                    custom_resolution="1280x720",
                    custom_fps=24.0,
                )
            )

        self.assertEqual(result.preview_path, "C:\\output\\deliverable_preview.mp4")
        self.assertIn("Delivery MP4:", result.status_text)
        self.assertIn("Browser Preview:", result.status_text)
        self.assertIn("Target Resolution: 1280x720 (custom)", result.status_text)


class GuiSmokeTests(unittest.TestCase):
    def test_create_ui_builds_when_gradio_is_available(self):
        try:
            gui = importlib.import_module("gui")
        except ModuleNotFoundError as exc:
            self.skipTest(f"GUI smoke test skipped: {exc}")

        app = gui.create_ui()
        self.assertEqual(type(app).__name__, "Blocks")

        try:
            loop = gui.asyncio.get_event_loop()
        except RuntimeError:
            return
        if not loop.is_closed():
            loop.close()


if __name__ == "__main__":
    unittest.main()
