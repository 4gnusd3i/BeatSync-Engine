# BeatSync WinUI Migration Handoff

## Current State

- Branch used during this migration: `dev-winui3`
- Portable Python backend refactor is already in place under `beatsync/`
- Gradio still exists as a fallback entrypoint and has not been retired yet
- WinUI desktop shell exists at `winui/BeatSync.Desktop/`
- WinUI app builds and launches locally against the portable Python backend
- Python bridge is active and used by WinUI through `beatsync_bridge.py`
- WinUI log area is split into:
  - left: render/status output
  - right: backend debug/stderr output
- Both log panes have fixed height and internal scrollbars
- Both log panes now auto-follow appended content

## Important Architecture Notes

- The portable runtime model is still the product constraint
- The WinUI app locates the repo root by checking for:
  - `bin/python-3.13.9-embed-amd64/python.exe`
  - `beatsync_bridge.py`
- Business logic remains in Python
- WinUI is only a native desktop client over the Python backend
- Bridge JSON goes over subprocess stdio
- Human/debug output comes from stderr and is shown in the WinUI debug pane

## Current Key Files

- Python backend contracts:
  - `beatsync/domain/models.py`
  - `beatsync/application/runtime_service.py`
  - `beatsync/application/render_service.py`
  - `beatsync/application/source_service.py`
- Python bridge:
  - `beatsync/bridge/cli.py`
  - `beatsync_bridge.py`
- Legacy Gradio adapter:
  - `beatsync/legacy_gradio/app.py`
  - `gui.py`
- WinUI app:
  - `winui/BeatSync.Desktop/BeatSync.Desktop.csproj`
  - `winui/BeatSync.Desktop/MainWindow.xaml.cs`
  - `winui/BeatSync.Desktop/ViewModels/MainViewModel.cs`
  - `winui/BeatSync.Desktop/Services/PythonBridgeService.cs`
  - `winui/BeatSync.Desktop/Services/RepoLocator.cs`
  - `winui/BeatSync.Desktop/Services/PickerService.cs`

## Current Validation Status

- Portable Python tests pass with the embedded interpreter:
  - `bin\python-3.13.9-embed-amd64\python.exe -m unittest discover -s dev -p "test_*.py" -v`
- WinUI app builds successfully:
  - `dotnet build winui\BeatSync.Desktop\BeatSync.Desktop.csproj -c Debug -p:Platform=x64`
- WinUI app has been launched and verified multiple times as a real desktop window
- Current WinUI work is functionally usable, but parity with the old Gradio app is not fully closed

## Remaining Work

### 1. Finish UI Parity Validation

- Run full manual parity checks between Gradio and WinUI for:
  - generation modes: `manual`, `smart`, `auto`
  - processing modes: `cpu`, `h264_nvenc`, `hevc_nvenc`, `prores_proxy`
  - quality presets and ProRes delivery MP4 toggle
  - output filename extension switching
  - source validation messages
  - preview-generation behavior
  - final status text content
- Confirm WinUI default values exactly match current backend/legacy behavior
- Confirm invalid input cases behave correctly in WinUI:
  - missing audio
  - missing folder
  - unsupported audio extension
  - empty clip folder
  - malformed FPS value

### 2. Thin The Legacy Gradio Layer Further

- `beatsync/legacy_gradio/app.py` still contains a lot of presentation logic and helper code
- Keep Gradio available until WinUI parity is fully signed off
- After parity is confirmed:
  - strip any remaining backend-like logic out of `beatsync/legacy_gradio/app.py`
  - verify `gui.py` is only a compatibility wrapper

### 3. Retire Gradio After Parity

- Remove Gradio as the primary UI path only after WinUI parity is confirmed
- Delete or archive:
  - `beatsync/legacy_gradio/`
  - `gui.py`
  - Gradio-only helpers and theme/html assets that are no longer used
- Update dependency manifests to remove `gradio` and Gradio-only transitive packages

### 4. Runtime And Dependency Cleanup

- Re-audit actual imports against:
  - `requirements/runtime.txt`
  - `requirements/dev.txt`
- Use:
  - `dev/audit_portable_runtime.py`
  - `dev/sync_portable_runtime.ps1`
- Remove orphaned packages from the portable `site-packages` only after verifying they are truly unused
- Keep portability intact:
  - no machine-wide Python dependency
  - no machine-wide FFmpeg dependency
  - no machine-wide CUDA dependency requirement for the shipped app

### 5. Portable WinUI Publish Path

- Validate the portable WinUI publish profile:
  - `winui/BeatSync.Desktop/Properties/PublishProfiles/portable-win-x64.pubxml`
- Confirm the self-contained publish output can run from a portable folder layout
- Confirm repo root discovery still works from the intended publish location
- If necessary, adjust publish layout so the WinUI executable and portable Python assets are easy to ship together

### 6. Documentation

- Update `README.md` with:
  - current WinUI build/run workflow
  - portable runtime expectations
  - Gradio transition status
  - release/publish workflow if finalized

### 7. Testing Expansion

- Add WinUI-level tests where practical for:
  - mode visibility changes
  - output filename switching
  - bridge error handling
  - debug log forwarding
  - command enable/disable behavior while rendering
- Keep Python tests centered on backend contracts rather than UI framework construction

## Recommended Next Step Order

1. Run a real parity pass in WinUI against the old Gradio behavior
2. Fix any parity regressions found during that pass
3. Validate the portable publish flow for the WinUI app
4. Update docs
5. Remove Gradio and prune dependencies only after parity and publish are both confirmed

## Parity Checklist

- Manual mode render works
- Smart mode render works
- Auto mode render works
- CPU render works
- NVENC H.264 render works
- NVENC HEVC render works
- ProRes render works
- ProRes delivery MP4 option works
- Preview file selection works
- Status log text looks correct
- Debug log panel shows stderr output
- Output filename changes `.mp4` <-> `.mov` correctly
- Path validation is equivalent to the old UI
- Output files land in expected portable output folders

## Known Practical Notes

- The WinUI shell currently lives mostly in `MainWindow.xaml.cs`
- `MainWindow.xaml` is intentionally minimal because earlier richer XAML/XAML-binding attempts triggered template-level compiler failures
- If revisiting the XAML structure later:
  - use a template-first recovery approach
  - keep changes incremental
  - rebuild often
- Do not assume a richer XAML refactor is free; verify after each step

## Build / Run Commands

### Python tests

`bin\python-3.13.9-embed-amd64\python.exe -m unittest discover -s dev -p "test_*.py" -v`

### WinUI build

`dotnet build winui\BeatSync.Desktop\BeatSync.Desktop.csproj -c Debug -p:Platform=x64`

### WinUI executable

`winui\BeatSync.Desktop\bin\x64\Debug\net8.0-windows10.0.19041.0\BeatSync.Desktop.exe`

## Commit Landmarks

- `50a823b` `Refactor Python backend into beatsync package`
- `ad42746` `Add WinUI desktop shell and bridge bindings`
- `4b25f3e` `Ignore WinUI build artifacts`
- `01bba75` `Split WinUI render and debug logs`
- `d696595` `Constrain WinUI log pane height`
- `08a76b6` `Increase WinUI log pane height`
- `5ab5c1c` `Auto-scroll WinUI log panes`
- `17bdc01` `Follow appended content in WinUI logs`

## Local Workspace Notes

- There is currently an unrelated local modification in `.gitignore`
- This file was intentionally left untouched during the WinUI work
- If continuing from another device, sync or inspect local-only workspace changes before rebasing or merging
