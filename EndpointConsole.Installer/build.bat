@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PROJECT=%SCRIPT_DIR%EndpointConsole.Installer.wixproj"
set "CONFIGURATION=%~1"
set "PRODUCT_VERSION=%~2"

if "%CONFIGURATION%"=="" set "CONFIGURATION=Release"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet CLI not found in PATH.
    exit /b 1
)

if not exist "%PROJECT%" (
    echo [ERROR] Installer project not found: %PROJECT%
    exit /b 1
)

echo Building installer...
echo   Project: %PROJECT%
echo   Configuration: %CONFIGURATION%
if not "%PRODUCT_VERSION%"=="" echo   ProductVersion: %PRODUCT_VERSION%

if "%PRODUCT_VERSION%"=="" (
    dotnet build "%PROJECT%" -c %CONFIGURATION%
) else (
    dotnet build "%PROJECT%" -c %CONFIGURATION% -p:ProductVersion=%PRODUCT_VERSION%
)

if errorlevel 1 (
    echo [ERROR] Installer build failed.
    exit /b 1
)

echo.
echo Build succeeded.
echo MSI path:
echo   %SCRIPT_DIR%bin\x64\%CONFIGURATION%\EndpointConsole.Installer.msi

endlocal
exit /b 0
