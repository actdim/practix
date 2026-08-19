@echo off
setlocal enabledelayedexpansion
title OpenObserve Server (port 5080)
echo Starting OpenObserve server on http://localhost:5080 ...
echo Default Login: root@example.com / Complexpass#123
echo Opening browser for OpenObserve GUI ...
echo Press Ctrl+C to stop the server.
echo.

cd /d "%~dp0"

set "EXE_PATH="

if exist "%~dp0openobserve.exe" set "EXE_PATH=%~dp0openobserve.exe"

if not defined EXE_PATH (
    for /f "delims=" %%f in ('dir /b /s "%~dp0*openobserve*.exe" 2^>nul') do (
        set "EXE_PATH=%%f"
    )
)

if not defined EXE_PATH (
    echo [WARNING] OpenObserve executable not found in %~dp0
    echo Downloading OpenObserve v0.92.2 ...
    call "%~dp0download-openobserve.cmd"
    
    if exist "%~dp0openobserve.exe" set "EXE_PATH=%~dp0openobserve.exe"
    if not defined EXE_PATH (
        for /f "delims=" %%f in ('dir /b /s "%~dp0*openobserve*.exe" 2^>nul') do (
            set "EXE_PATH=%%f"
        )
    )
)

if not defined EXE_PATH (
    echo [ERROR] Could not find or download OpenObserve executable.
    pause
    exit /b 1
)

set "ZO_DATA_DIR=%~dp0data"
set "ZO_HTTP_PORT=5080"
set "ZO_ROOT_USER_EMAIL=root@example.com"
set "ZO_ROOT_USER_PASS=Complexpass#123"

start "" "http://localhost:5080"

echo Launching: !EXE_PATH!
"!EXE_PATH!"
