param(
    [switch]$IncludeDev
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$python = Join-Path $repoRoot "bin\python-3.13.9-embed-amd64\python.exe"
$runtimeRequirements = Join-Path $repoRoot "requirements\runtime.txt"
$devRequirements = Join-Path $repoRoot "requirements\dev.txt"

if (-not (Test-Path $python)) {
    throw "Portable Python not found at $python"
}

& $python -m pip install --upgrade pip
& $python -m pip install -r $runtimeRequirements

if ($IncludeDev) {
    & $python -m pip install -r $devRequirements
}

& $python -m pip check

