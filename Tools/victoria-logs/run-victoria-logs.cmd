@echo off
title VictoriaLogs Server (port 9428)
echo Starting VictoriaLogs server on http://localhost:9428 ...
echo Press Ctrl+C to stop the server.
echo.

cd /d "%~dp0"

if exist "victoria-logs-windows-amd64-prod.exe" (
    "victoria-logs-windows-amd64-prod.exe" -storageDataPath "data" -httpListenAddr ":9428" -retentionPeriod 1d
) else if exist "victoria-logs-windows-amd64.exe" (
    "victoria-logs-windows-amd64.exe" -storageDataPath "data" -httpListenAddr ":9428" -retentionPeriod 1d
) else if exist "victoria-logs.exe" (
    "victoria-logs.exe" -storageDataPath "data" -httpListenAddr ":9428" -retentionPeriod 1d
) else (
    echo [ERROR] VictoriaLogs executable not found in this folder.
    echo Please ensure victoria-logs-windows-amd64-prod.exe or victoria-logs.exe is present in %~dp0
    pause
)
