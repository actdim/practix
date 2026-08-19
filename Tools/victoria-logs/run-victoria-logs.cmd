@echo off
setlocal enabledelayedexpansion
title VictoriaLogs Server (port 9428)
echo Starting VictoriaLogs server on http://localhost:9428 ...
echo Opening browser for VictoriaLogs GUI ...
echo Press Ctrl+C to stop the server.
echo.

cd /d "%~dp0"

set "EXE_PATH="

if exist "%~dp0victoria-logs-windows-amd64-prod.exe" set "EXE_PATH=%~dp0victoria-logs-windows-amd64-prod.exe"
if not defined EXE_PATH if exist "%~dp0victoria-logs-windows-amd64-v1.51.1-enterprise.exe" set "EXE_PATH=%~dp0victoria-logs-windows-amd64-v1.51.1-enterprise.exe"
if not defined EXE_PATH if exist "%~dp0victoria-logs-windows-amd64.exe" set "EXE_PATH=%~dp0victoria-logs-windows-amd64.exe"
if not defined EXE_PATH if exist "%~dp0victoria-logs.exe" set "EXE_PATH=%~dp0victoria-logs.exe"

if not defined EXE_PATH (
    for /f "delims=" %%f in ('dir /b /s "%~dp0*victoria-logs*.exe" 2^>nul') do (
        set "EXE_PATH=%%f"
    )
)

if not defined EXE_PATH (
    echo [WARNING] VictoriaLogs executable not found in %~dp0
    echo Downloading VictoriaLogs Enterprise v1.51.1 ...
    call "%~dp0download-victoria-logs.cmd"
    
    if exist "%~dp0victoria-logs-windows-amd64-prod.exe" set "EXE_PATH=%~dp0victoria-logs-windows-amd64-prod.exe"
    if not defined EXE_PATH (
        for /f "delims=" %%f in ('dir /b /s "%~dp0*victoria-logs*.exe" 2^>nul') do (
            set "EXE_PATH=%%f"
        )
    )
)

if not defined EXE_PATH (
    echo [ERROR] Could not find or download VictoriaLogs executable.
    pause
    exit /b 1
)

start "" "http://localhost:9428/select/vmui"

echo Launching: !EXE_PATH!
"!EXE_PATH!" -storageDataPath "%~dp0data" -httpListenAddr ":9428" -retentionPeriod 1d
