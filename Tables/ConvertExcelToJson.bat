@echo off
setlocal

set "ROOT=%~dp0"
set "TOOL=%ROOT%tool"
set "EXE=%TOOL%\ExcelToJson.exe"
set "SETTINGS=%TOOL%\Settings.ini"

if not exist "%EXE%" (
    echo [ERROR] ExcelToJson.exe not found: "%EXE%"
    pause
    exit /b 1
)

if not exist "%SETTINGS%" (
    echo [INFO] Settings.ini not found. Create default settings: "%SETTINGS%"
    > "%SETTINGS%" echo [Path]
    >> "%SETTINGS%" echo ExcelPath=..
    >> "%SETTINGS%" echo OutputPath=..\Json
    >> "%SETTINGS%" echo EnumExcelPath=Enum.xlsx
    >> "%SETTINGS%" echo.
    >> "%SETTINGS%" echo [Sheet]
    >> "%SETTINGS%" echo DataSheetName=Data
    >> "%SETTINGS%" echo.
    >> "%SETTINGS%" echo [Setting]
    >> "%SETTINGS%" echo IndexSubjectName=Index
    >> "%SETTINGS%" echo GroupSubjectName=Group
    >> "%SETTINGS%" echo IndexCell=0
    >> "%SETTINGS%" echo.
    >> "%SETTINGS%" echo [Data]
    >> "%SETTINGS%" echo SubjectRow=1
    >> "%SETTINGS%" echo ReferenceRow=2
    >> "%SETTINGS%" echo TypeRow=3
    >> "%SETTINGS%" echo DataRow=4
    >> "%SETTINGS%" echo.
    >> "%SETTINGS%" echo [Enum]
    >> "%SETTINGS%" echo NameRow=0
    >> "%SETTINGS%" echo DataRow=1
    >> "%SETTINGS%" echo.
    >> "%SETTINGS%" echo [Localize]
    >> "%SETTINGS%" echo SubjectRow=0
    >> "%SETTINGS%" echo DataRow=1
    >> "%SETTINGS%" echo.
    >> "%SETTINGS%" echo [Exclude]
    >> "%SETTINGS%" echo Files=
)

pushd "%ROOT%" >nul
"%EXE%" --settings "%SETTINGS%" %*
set "EXIT_CODE=%ERRORLEVEL%"
popd >nul

if not "%EXIT_CODE%"=="0" (
    echo.
    echo [ERROR] ExcelToJson failed. ExitCode=%EXIT_CODE%
)

echo.
echo Press any key to close this window.
pause >nul

exit /b %EXIT_CODE%
