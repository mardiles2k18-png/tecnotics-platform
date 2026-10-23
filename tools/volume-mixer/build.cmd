@echo off
rem Compila VolumeMixer.exe (requiere el SDK de .NET: https://dotnet.microsoft.com/download)
cd /d "%~dp0"
dotnet build -c Release || exit /b 1
echo.
echo Listo: %~dp0bin\Release\net48\VolumeMixer.exe
