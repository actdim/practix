@echo off
setlocal enabledelayedexpansion
title Seq Server (port 5341)
echo Starting Seq server on http://localhost:5341 ...
echo Opening browser for Seq Web GUI ...
echo Press Ctrl+C to stop the server.
echo.

cd /d "%~dp0"

set "EXE_PATH="

if exist "%~dp0seq.exe" set "EXE_PATH=%~dp0seq.exe"

if not defined EXE_PATH (
    for /f "delims=" %%f in ('dir /b /s "%~dp0*seq*.exe" 2^>nul') do (
        set "EXE_PATH=%%f"
    )
)

if not defined EXE_PATH (
    echo [WARNING] Seq executable not found in %~dp0
    echo Downloading Seq CLI ...
    call "%~dp0download-seq.cmd"
    
    if exist "%~dp0seq.exe" set "EXE_PATH=%~dp0seq.exe"
    if not defined EXE_PATH (
        for /f "delims=" %%f in ('dir /b /s "%~dp0*seq*.exe" 2^>nul') do (
            set "EXE_PATH=%%f"
        )
    )
)

if not defined EXE_PATH (
    echo [ERROR] Could not find or download Seq executable.
    pause
    exit /b 1
)

start "" "http://localhost:5341"

echo Launching: !EXE_PATH!
"!EXE_PATH!" run --storage="%~dp0data" --listen="http://localhost:5341"
