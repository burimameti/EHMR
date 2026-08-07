@echo off
REM ===========================================================================
REM  Гради инсталер за EHMR во два чекора: објавување, па пакување.
REM  Се пушта со двоен клик или од коренот на проектот:  Installer\build-installer.cmd
REM ===========================================================================

setlocal

REM Оди во коренот на проектот, без разлика од каде е пуштена скриптата.
cd /d "%~dp0.."

REM ISCC.exe стои во „Program Files" или во „Program Files (x86)",
REM и верзијата се менува — затоа се бара наместо да се зашива.
set "ISCC="
for %%V in (7 6 5) do (
    if not defined ISCC if exist "%ProgramFiles%\Inno Setup %%V\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup %%V\ISCC.exe"
    if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup %%V\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup %%V\ISCC.exe"
)

if not defined ISCC (
    echo.
    echo [ГРЕШКА] Inno Setup не е пронајден.
    echo          Преземи го од https://jrsoftware.org/isdl.php
    echo.
    exit /b 1
)

echo.
echo === 1/2  Се објавува апликацијата ===
echo.

dotnet publish EHMR.csproj -f net9.0-windows10.0.19041.0 -c Release -r win10-x64 --self-contained true -p:WindowsPackageType=None

if errorlevel 1 (
    echo.
    echo [ГРЕШКА] Објавувањето не успеа. Инсталерот не е граден.
    exit /b 1
)

echo.
echo === 2/2  Се гради инсталерот ===
echo     %ISCC%
echo.

"%ISCC%" "Installer\EHMR.iss"

if errorlevel 1 (
    echo.
    echo [ГРЕШКА] Градењето на инсталерот не успеа.
    exit /b 1
)

echo.
echo === ГОТОВО ===
echo Инсталерот е во:  Installer\Output\
echo.

endlocal
