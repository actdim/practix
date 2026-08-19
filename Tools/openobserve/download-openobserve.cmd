@echo off
title Download OpenObserve
echo ========================================================
echo  Downloading OpenObserve v0.92.2 for Windows
echo ========================================================
echo.

cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0download-openobserve.ps1"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUCCESS] OpenObserve downloaded and extracted successfully!
    echo You can now run run-openobserve.cmd to start the server.
) else (
    echo.
    echo [ERROR] Download failed. Please check your network connection.
)
pause
