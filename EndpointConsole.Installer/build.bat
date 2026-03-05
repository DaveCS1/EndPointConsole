@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PROJECT=%SCRIPT_DIR%EndpointConsole.Installer.wixproj"
set "WPF_PROJECT=%SCRIPT_DIR%..\EndpointConsole.Wpf\EndpointConsole.Wpf.csproj"
set "PUBLISH_DIR=%SCRIPT_DIR%..\artifacts\publish\EndpointConsole.Wpf"
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

if not exist "%WPF_PROJECT%" (
    echo [ERROR] WPF project not found: %WPF_PROJECT%
    exit /b 1
)

echo Publishing WPF app...
echo   Project: %WPF_PROJECT%
echo   PublishDir: %PUBLISH_DIR%
dotnet publish "%WPF_PROJECT%" -c %CONFIGURATION% -o "%PUBLISH_DIR%" -p:SelfContained=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
if errorlevel 1 (
    echo [ERROR] WPF publish failed.
    exit /b 1
)

echo Building installer...
echo   Project: %PROJECT%
echo   Configuration: %CONFIGURATION%
if not "%PRODUCT_VERSION%"=="" echo   ProductVersion: %PRODUCT_VERSION%

if "%PRODUCT_VERSION%"=="" (
    dotnet build "%PROJECT%" -c %CONFIGURATION% -p:AppPublishDir="%PUBLISH_DIR%"
) else (
    dotnet build "%PROJECT%" -c %CONFIGURATION% -p:ProductVersion=%PRODUCT_VERSION% -p:AppPublishDir="%PUBLISH_DIR%"
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
