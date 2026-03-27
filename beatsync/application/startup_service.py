"""Startup cleanup services."""

from __future__ import annotations

import os
import shutil

from ..processing.renderer import get_local_temp_dir


def cleanup_on_startup() -> None:
    session_temp_base = get_local_temp_dir()
    try:
        if os.path.exists(session_temp_base):
            print("Cleaning up old session directories...")
            for item in os.listdir(session_temp_base):
                item_path = os.path.join(session_temp_base, item)
                try:
                    if os.path.isdir(item_path):
                        shutil.rmtree(item_path, ignore_errors=True)
                        print(f"   Removed: {item}")
                except Exception as exc:
                    print(f"   Could not remove {item}: {exc}")
            print("   Old sessions cleared.")
        else:
            os.makedirs(session_temp_base, exist_ok=True)
            print("   Created session temp directory")
    except Exception as exc:
        print(f"   Warning: Could not clean up sessions: {exc}")
        os.makedirs(session_temp_base, exist_ok=True)

