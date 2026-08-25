@echo off
setlocal
title Update NuGet Packages (Central Package Management)
echo Installing/Verifying dotnet-outdated tool...
echo Updating package versions centrally in Directory.Packages.props...
echo.

cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0update-packages.ps1"
