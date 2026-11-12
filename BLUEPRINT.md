# SafeKeyBinder — Architectural Blueprint

- **Version:** v1.1.3
- **Last Updated:** 2026-11-12
- **Platform:** Windows Desktop (.NET Framework 4.x / C# 5, `csc.exe` native build)

---

## 1. Struktur Arsitektur

Aplikasi mengikuti pemisahan tanggung jawab (*Separation of Concerns*) agar mudah dibaca, diaudit, dan diuji:

```text
SafeKeyBinder/
├── assets/
│   └── app.ico                      # Ikon kustom multi-resolusi (16x16 - 256x256)
├── build.bat                        # Skrip kompilasi lokal memakai csc.exe bawaan Windows
├── SafeKeyBinder.exe                # Binary hasil kompilasi lokal (dengan embedded Win32 icon)
├── SafeKeyBinder.config.xml         # Konfigurasi & daftar keybind lokal (dibuat otomatis)
├── src/
│   ├── Program.cs                   # Entry point, DPI awareness, & dependency wiring
│   ├── Models/
│   │   ├── AppConfig.cs             # Model konfigurasi global & status runtime
│   │   ├── AutoSequenceConfig.cs    # Model pola & pengaturan Auto Sequence
│   │   └── KeybindItem.cs           # Model keybind & formatter nama tombol
│   ├── Native/
│   │   └── Win32Native.cs           # Deklarasi P/Invoke user32.dll & kernel32.dll (tanpa API jaringan)
│   ├── Services/
│   │   ├── AutoSequenceService.cs   # Runner latar belakang untuk Auto Sequence + Pause/Stop
│   │   ├── ConfigService.cs         # Persistensi konfigurasi XML atomik lokal
│   │   ├── WindowService.cs         # Enumerasi jendela aktif & proses Windows
│   │   ├── KeybindMatcher.cs        # Logika pencocokan hotkey, scope, & toggle (pure logic)
│   │   ├── SequenceGenerator.cs     # Generator pola huruf, angka, & daftar manual
│   │   ├── InputSenderService.cs    # Pengiriman teks Unicode & tombol virtual via SendInput
│   │   └── KeyboardHookService.cs   # Hook WH_KEYBOARD_LL + background queue worker
│   └── UI/
│       ├── ThemeTokens.cs           # Token desain HIG (warna, tipografi, spacing)
│       ├── KeyCaptureDialog.cs      # Dialog perekam kombinasi tombol
│       ├── SecurityAuditDialog.cs   # Ringkasan jaminan keamanan & akses cepat ke folder src/
│       ├── AutoSequenceControl.cs   # Tab antarmuka Auto Sequence & antrean urutan
│       └── MainForm.cs              # Antarmuka utama bertab (Manual KeyBinder & Auto Sequence)
└── tests/
    ├── run_tests.bat                # Runner pengujian otomatis
    └── SafeKeyBinderTests.cs        # Unit, integration, dan security audit test suite
```

---

## 2. Feature Modules

- **Models (`KeybindItem`, `AppConfig`):** Mendefinisikan struktur data keybind (hotkey, modifier, teks, prefix/suffix) serta preferensi global (*Global* vs *Window Filter*, *Master Toggle*, *Typing Delay*, dan *Typing Pause* opsional).
- **Native Bridge (`Win32Native`):** Mengisolasi seluruh P/Invoke `WH_KEYBOARD_LL`, `SendInput`, `GetForegroundWindow`, dan `EnumWindows` pada satu berkas terpisah agar audit keamanan dapat dilakukan dalam hitungan detik.
- **Input & Hook Pipeline (`KeyboardHookService`, `KeybindMatcher`, `InputSenderService`):**
  - *Hook* mengabaikan input sintetik (`LLKHF_INJECTED`) untuk mencegah loop tak terbatas.
  - Pencocokan berjalan di memori RAM secara instan; pengiriman teks diproses pada *worker thread* terpisah agar *hook* tidak pernah terkena *timeout* Windows.
  - Sebelum mengetik, modifier fisik yang sedang ditekan dilepas sementara secara aman agar kombinasi seperti `Ctrl + F9` tidak memicu pintasan yang salah pada aplikasi target.
- **Local Persistence (`ConfigService`):** Menyimpan konfigurasi secara atomik (`.tmp` -> replace) ke XML standar yang mudah dibaca manusia.
- **Desktop UI (`MainForm`, `AutoSequenceControl`, `KeyCaptureDialog`, `SecurityAuditDialog`, `ThemeTokens`):** Antarmuka Windows Forms bertab dengan tab **KeyBinder (Manual)** serta tab **Auto Sequence (Urutan Otomatis)** lengkap dengan generator pola, preview langkah, hotkey cepat `F6`/`F7`, dan pengaman fokus jendela target.
