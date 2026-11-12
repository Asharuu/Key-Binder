@echo off
setlocal
cd /d "%~dp0.."

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if not exist "tests\bin" mkdir "tests\bin"

"%CSC%" ^
  /nologo ^
  /target:exe ^
  /out:"tests\bin\SafeKeyBinderTests.exe" ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Xml.dll ^
  "tests\SafeKeyBinderTests.cs" ^
  "src\Models\KeybindItem.cs" ^
  "src\Models\AutoSequenceConfig.cs" ^
  "src\Models\AppConfig.cs" ^
  "src\Native\Win32Native.cs" ^
  "src\Services\ConfigService.cs" ^
  "src\Services\WindowService.cs" ^
  "src\Services\KeybindMatcher.cs" ^
  "src\Services\SequenceGenerator.cs" ^
  "src\Services\InputSenderService.cs" ^
  "src\Services\KeyboardHookService.cs" ^
  "src\Services\AutoSequenceService.cs" ^
  "src\UI\ThemeTokens.cs" ^
  "src\UI\KeyCaptureDialog.cs" ^
  "src\UI\SecurityAuditDialog.cs" ^
  "src\UI\AutoSequenceControl.cs" ^
  "src\UI\MainForm.cs"

if errorlevel 1 (
    echo [ERROR] Test compilation failed.
    exit /b 1
)

"tests\bin\SafeKeyBinderTests.exe"
exit /b %errorlevel%
