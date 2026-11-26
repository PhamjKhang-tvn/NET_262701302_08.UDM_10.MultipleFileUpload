@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Build.ps1" 
if errorlevel 1 (
 echo.
 echo FAILED. Read the error above and Build logs.
 pause
 exit /b 1
)
echo.
echo Finished.
pause
