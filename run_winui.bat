@echo off
chcp 65001 >nul
setlocal

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

set "PORTABLE_PYTHON=%SCRIPT_DIR%bin\python-3.13.9-embed-amd64\python.exe"
set "PROJECT=%SCRIPT_DIR%winui\BeatSync.Desktop\BeatSync.Desktop.csproj"
set "OUTPUT_EXE=%SCRIPT_DIR%winui\BeatSync.Desktop\bin\x64\Debug\net8.0-windows10.0.19041.0\BeatSync.Desktop.exe"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo dotnet SDK was not found on PATH.
    exit /b 1
)

if not exist "%PORTABLE_PYTHON%" (
    echo Portable Python not found at:
    echo   %PORTABLE_PYTHON%
    echo Populate bin\ first. See bin\README.md.
    exit /b 1
)

set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_CLI_HOME=%SCRIPT_DIR%.dotnet"

echo Building BeatSync Desktop...
dotnet build "%PROJECT%" -c Debug -p:Platform=x64
if errorlevel 1 exit /b 1

if not exist "%OUTPUT_EXE%" (
    echo Expected WinUI executable was not found at:
    echo   %OUTPUT_EXE%
    exit /b 1
)

echo Launching BeatSync Desktop...
start "" "%OUTPUT_EXE%"
