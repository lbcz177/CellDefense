@echo off
setlocal
set "CELL_DEFENSE_UNITY=E:\UnityEditors\2022.3.53f1c1\Editor\Unity.exe"
if not exist "%CELL_DEFENSE_UNITY%" (
    echo Unity 2022.3.53f1c1 was not found at "%CELL_DEFENSE_UNITY%".
    pause
    exit /b 1
)
start "" "%CELL_DEFENSE_UNITY%" -projectPath "%~dp0."
