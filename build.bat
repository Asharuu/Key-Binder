@echo off
setlocal
cd /d "%~dp0"

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if not exist "%CSC%" (
    echo [ERROR] Compiler C# bawaan Windows ^(csc.exe^) tidak ditemukan.
    exit /b 1
)

echo [SafeKeyBinder] Mengompilasi source code lokal dengan %CSC% ...

"%CSC%" ^
  /nologo ^
  /target:winexe ^
  /optimize+ ^
  /win32icon:"assets\app.ico" ^
  /out:"SafeKeyBinder.exe" ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Xml.dll ^
  "src\Program.cs" ^
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
    echo [ERROR] Kompilasi gagal!
    exit /b 1
)

echo [OK] Kompilasi berhasil: SafeKeyBinder.exe siap digunakan.
exit /b 0
