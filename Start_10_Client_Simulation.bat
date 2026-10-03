@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Launch.ps1" -Mode Simulator
if errorlevel 1 pause
