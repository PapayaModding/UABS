
@echo off
setlocal
title UABS Unity AssetBundle Generator

cd /d "%~dp0"

set "UNITY=E:\App\Unity\Hub\Editor\2021.3.45f2\Editor\Unity.exe"
set "TOOL=test-fixtures\unity\generator\tools\fixturectl.py"

echo ==========================================
echo       UABS AssetBundle Generator
echo ==========================================
echo.

if not exist "%UNITY%" (
    echo ERROR: Unity Editor not found:
    echo %UNITY%
    echo.
    echo Edit the UNITY path in this BAT file.
    goto end
)

if not exist "%TOOL%" (
    echo ERROR: Generator script not found.
    echo Run this BAT file from the UABS repository root.
    goto end
)

echo Select generation profile:
echo.
echo [1] Smoke      - 3 bundles
echo [2] Regression - 18 bundles
echo [3] Stress     - 250 bundles
echo [4] Exit
echo.

choice /c 1234 /n /m "Choose [1-4]: "

if errorlevel 4 goto end
if errorlevel 3 set "PROFILE=stress"
if errorlevel 2 if not defined PROFILE set "PROFILE=regression"
if errorlevel 1 if not defined PROFILE set "PROFILE=smoke"

echo.
echo Generating %PROFILE% bundles...
echo.

where py >nul 2>&1
if not errorlevel 1 (
    set "PYTHON=py -3"
) else (
    set "PYTHON=python"
)

%PYTHON% "%TOOL%" generate --profile %PROFILE% --unity "%UNITY%"

if errorlevel 1 (
    echo.
    echo ERROR: Generation failed.
    goto end
)

echo.
echo Generation completed. Verifying files...
echo.

%PYTHON% "%TOOL%" verify --manifest "test-fixtures/unity/generator/Generated/working/%PROFILE%/fixture-manifest.json"

if errorlevel 1 (
    echo.
    echo ERROR: Verification failed.
    goto end
)

echo.
echo SUCCESS! Generated bundles are located at:
echo %CD%\test-fixtures\unity\generator\Generated\working\%PROFILE%\bundles
echo.
echo NOTE: Nothing has been pushed to GitHub.

:end
echo.
pause
endlocal
