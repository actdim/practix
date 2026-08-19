@echo off
title Download VictoriaLogs
echo ========================================================
echo  Downloading VictoriaLogs for Windows
echo ========================================================
echo.

cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0download-victoria-logs.ps1"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUCCESS] VictoriaLogs downloaded and extracted successfully!
    echo You can now run run-victoria-logs.cmd to start the server.
) else (
    echo.
    echo [ERROR] Download failed. Please check your network connection.
)
pause
