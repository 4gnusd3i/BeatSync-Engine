"""JSON CLI bridge for WinUI and other native clients."""

from __future__ import annotations

import argparse
import json
import sys
from contextlib import redirect_stdout
from dataclasses import asdict, is_dataclass
from enum import Enum

from ..application.path_utils import recommend_output_filename
from ..application.render_service import process_render
from ..application.runtime_service import probe_runtime_status
from ..application.source_service import inspect_sources
from ..domain.models import RenderRequest


def _to_jsonable(value):
    if is_dataclass(value):
        return _to_jsonable(asdict(value))
    if isinstance(value, Enum):
        return value.value
    if isinstance(value, dict):
        return {key: _to_jsonable(item) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [_to_jsonable(item) for item in value]
    return value


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="BeatSync bridge CLI")
    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser("probe-runtime", help="Return runtime capability status.")

    inspect_parser = subparsers.add_parser("inspect-sources", help="Validate and inspect source paths.")
    inspect_parser.add_argument("--audio-path", default="")
    inspect_parser.add_argument("--video-folder", default="")

    recommend_parser = subparsers.add_parser("recommend-output-name", help="Return the backend output filename recommendation.")
    recommend_parser.add_argument("--current-filename", default="")
    recommend_parser.add_argument("--processing-mode", required=True)

    render_parser = subparsers.add_parser("render", help="Run a full render request from JSON.")
    render_parser.add_argument("--request-json", default=None)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    with redirect_stdout(sys.stderr):
        if args.command == "probe-runtime":
            payload = probe_runtime_status()
        elif args.command == "inspect-sources":
            payload = inspect_sources(args.audio_path, args.video_folder)
        elif args.command == "recommend-output-name":
            payload = {
                "recommended_filename": recommend_output_filename(
                    args.current_filename,
                    args.processing_mode,
                )
            }
        else:
            raw_request = args.request_json if args.request_json else sys.stdin.read()
            request_data = json.loads(raw_request)
            payload = process_render(RenderRequest(**request_data))

    sys.stdout.write(json.dumps(_to_jsonable(payload), ensure_ascii=False))
    sys.stdout.write("\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
