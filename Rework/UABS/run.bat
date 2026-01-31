@echo off
REM Clean projects
dotnet clean Core\Core.csproj
dotnet clean AvaloniaUI\UABS.csproj

REM Build Core first
dotnet build Core\Core.csproj
IF ERRORLEVEL 1 (
    echo Core build failed. Exiting.
    exit /b 1
)

REM Build AvaloniaUI
dotnet build AvaloniaUI\UABS.csproj
IF ERRORLEVEL 1 (
    echo AvaloniaUI build failed. Exiting.
    exit /b 1
)

REM Run AvaloniaUI
dotnet run --project AvaloniaUI\UABS.csproj