# Portable Runtime Layout

`bin/` is intentionally kept out of Git because the portable runtime payload is large and machine-local.

After pulling this branch on another workstation, populate `bin/` with the repo-local runtime before running BeatSync.

Expected layout:

- `bin/python-3.13.9-embed-amd64/python.exe`
- `bin/ffmpeg/ffmpeg.exe`
- `bin/ffmpeg/ffprobe.exe`
- `bin/CUDA/<version>/` if GPU analysis or NVENC workflows are needed

Recommended workflow on another workstation:

1. Copy `bin/` from a known-good BeatSync checkout, or recreate the same layout manually.
2. Run `dev\sync_portable_runtime.ps1 -IncludeDev` to align the embedded Python packages with `requirements/`.
3. Use `run_winui.bat` for the native client or `run.bat` for the temporary Gradio fallback.

Notes:

- Keep only one CuPy CUDA package line installed at a time.
- `bin/CUDA/v12.9.x` is still the recommended portable CUDA line for Pascal GPUs such as the GTX 1080 Ti.
- The WinUI app locates the repo root by looking for both `bin/python-3.13.9-embed-amd64/python.exe` and `beatsync_bridge.py`.
