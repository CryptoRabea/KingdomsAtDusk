@echo off
setlocal enabledelayedexpansion

:: Paths
set "source=D:\Work\UnityProjects\KingdomsAtDusk"
set "destination=D:\Work\UnityProjects\KingdomsAtDusk\mdFiles"
set "logFile=%destination%\copy_log.txt"

echo Preparing destination folder...
mkdir "%destination%" >nul 2>&1

echo Clearing old log...
echo ==== MD File Copy Log ==== > "%logFile%"
echo Source: %source% >> "%logFile%"
echo Destination: %destination% >> "%logFile%"
echo ========================== >> "%logFile%"
echo. >> "%logFile%"

echo Counting .md files (excluding Unity garbage folders)...

:: Count total files for progress bar
for /f %%A in ('powershell -NoProfile -Command ^
    "$src='%source%';" ^
    "Get-ChildItem -Path $src -Filter *.md -Recurse | Where-Object {" ^
    "  $_.FullName -notmatch 'Library|Temp|Logs|Obj|Build|UserSettings|.git|.vscode|Packages[/\\]manifest|Packages[/\\]cache' " ^
    "} | Measure-Object | %%{$_.Count}"') do set total=%%A

if %total%==0 (
    echo No .md files found!
    pause
    exit /b
)

echo Found %total% files.
echo.

set count=0

echo Copying files...

:: Main loop
powershell -NoProfile -Command ^
  "$src='%source%'; $dst='%destination%'; $log='%logFile%';" ^
  "$files = Get-ChildItem -Path $src -Filter *.md -Recurse | Where-Object {" ^
  "  $_.FullName -notmatch 'Library|Temp|Logs|Obj|Build|UserSettings|.git|.vscode|Packages[/\\]manifest|Packages[/\\]cache' };" ^
  "$i = 0;" ^
  "foreach ($f in $files) {" ^
  "    $i++;" ^
  "    $rel = $f.FullName.Substring($src.Length);" ^
  "    $target = Join-Path $dst (Split-Path $rel -Parent);" ^
  "    New-Item -ItemType Directory -Path $target -Force | Out-Null;" ^
  "    Copy-Item $f.FullName -Destination $target -Force;" ^
  "    Add-Content -Path $log -Value ('Copied: ' + $f.FullName);" ^
  "    $percent = [math]::Round(($i / $files.Count) * 100);" ^
  "    Write-Host ('Progress: ' + $percent + '%%') -ForegroundColor Cyan;" ^
  "} "

echo.
echo Done!
echo Log saved to: %logFile%
pause
