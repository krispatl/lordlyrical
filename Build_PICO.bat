@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Launch.ps1" -Mode BuildPico
if errorlevel 1 pause
