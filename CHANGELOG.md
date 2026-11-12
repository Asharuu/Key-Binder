# Changelog

Semua perubahan penting pada proyek **SafeKeyBinder** dicatat pada file ini.

## [v1.1.3] - 2026-11-12

### Added
- **Ikon Aplikasi Multi-Resolusi Kustom (`assets/app.ico`, `build.bat`, `ThemeTokens`, `MainForm`):** Menambahkan ikon aplikasi vektor modern (`16x16` hingga `256x256` dengan desain 3D *mechanical keycap* `/` + `⚡`) yang disematkan langsung ke dalam binary `.exe` (`/win32icon`), *title bar* jendela utama, *Taskbar*, serta *shortcut* Desktop.

## [v1.1.2] - 2026-11-09

### Fixed
- **Dukungan Hardware ScanCode (`KEYEVENTF_SCANCODE`) untuk Game 3D / Roblox (`Win32Native`, `InputSenderService`):** Mengirim tombol virtual (`Prefix` seperti `/` dan `Suffix` seperti `Enter`) menggunakan flag hardware `KEYEVENTF_SCANCODE` (`0x0008`) dengan durasi tahan `35 ms` (~2 frame pada 60 FPS) agar engine DirectInput/Roblox dapat mendeteksi pembukaan menu chat dan pengiriman `Enter` secara konsisten.

## [v1.1.1] - 2026-11-08

### Fixed
- **Pemilihan Posisi & Muat Ulang Antrean Saat Pause (`AutoSequenceService`, `AutoSequenceControl`):**
  - Pengguna kini bisa langsung mengklik baris mana pun di daftar **Antrean**, menekan **Reset ke #1**, maupun menekan **Generate & Muat ke Antrean** saat status sedang **DIJEDA (PAUSE)** tanpa harus menekan **Stop** lebih dulu.
  - Menekan tombol **Lanjutkan** dari UI kini otomatis menjalankan **Countdown** agar pengguna sempat berpindah kembali ke jendela target (sedangkan `F6` di dalam game tetap melanjutkan secara instan).
  - Baris **Simulasi langkah** kini selalu ikut diperbarui mengikuti posisi antrean aktif.

## [v1.1.0] - 2026-11-08

### Added
- **Tab Auto Sequence / Urutan Otomatis (`AutoSequenceConfig`, `SequenceGenerator`, `AutoSequenceService`, `AutoSequenceControl`):**
  - **Generator Pola Fleksibel:** Mendukung generator rentang huruf (`77a`–`77z`, `aa`–`zz`), rentang angka dengan opsi *zero-padding* (`001`–`050`), serta editor daftar manual per baris.
  - **Template Perintah `{item}` & Timing Lengkap:** Pengaturan format perintah (`warp {item}` / `/warp {item}`), *Prefix Key* + *Jeda Prefix (ms)*, *Suffix* (`Enter`/`Tab`/`Space`), *Delay Huruf (ms)*, *Jeda Antar Item (ms)*, *Countdown Awal (detik)*, serta opsi *Loop*.
  - **Kontrol Hotkey Cepat & Pengaman Jendela Target:** Hotkey global `F6` (*Pause/Resume*) dan `F7` (*Stop*) yang responsif meski saat jeda panjang, plus opsi pengaman *Hanya ketik jika jendela target fokus*.
- **Thread-Safe Input Sending (`InputSenderService`):** Menambahkan sinkronisasi *lock* agar eksekusi keybind manual dan Auto Sequence tidak saling bertabrakan.

## [v1.0.1] - 2026-11-08

### Fixed
- **Perbaikan Tampilan High-DPI & Pemotongan Teks (`ThemeTokens`, `MainForm`, `KeyCaptureDialog`, `SecurityAuditDialog`):** Mengganti ukuran piksel statis menjadi layout berbasis `AutoSize` + skala DPI (`ThemeTokens.Scale`), serta menonaktifkan `UseMnemonic` agar karakter `&` tidak hilang dan seluruh tombol/label tampil utuh di layar 125%–150% DPI.
- **Keandalan Pengiriman Teks di Game (`InputSenderService`, `AppConfig`):** Mengirim karakter satu per satu dengan jeda minimum aman (5 ms untuk macro singkat meski `Delay (ms) = 0`) serta menahan *virtual key* (`Prefix` / `Suffix`) selama 15 ms agar huruf tidak terlewat di aplikasi/game seperti Roblox.
- **Event `ItemChecked` Saat Startup (`MainForm`):** Memasang listener `ItemChecked` setelah form selesai ditampilkan sehingga status footer awal tetap akurat.

## [v1.0.0] - 2026-11-08

### Added
- **Universal KeyBinder Engine (`KeyboardHookService`, `KeybindMatcher`, `InputSenderService`):** Global keyboard hook (`WH_KEYBOARD_LL`) dan simulasi input Unicode (`SendInput` dengan `KEYEVENTF_UNICODE`) tanpa menimpa isi *clipboard*.
- **Manajemen Keybind Dinamis:** Tambah, edit, duplikat, hapus, serta toggle cepat aktif/nonaktif lewat `ListView` bercentang.
- **Hotkey & Key Capture Dialog (`KeyCaptureDialog`):** Perekam tombol interaktif yang mendukung tombol fungsi, huruf/angka, Numpad, navigasi, serta kombinasi `Ctrl`, `Alt`, dan `Shift`.
- **Opsi Universal Fleksibel:** Dukungan teks multi-baris, *Prefix Key* opsional, *Suffix* (`Tidak ada`, `Enter`, `Tab`, `Space`), pengaturan *Typing Delay (ms)*, serta *Master Toggle Hotkey*.
- **Target Aplikasi & Typing Pause Opsional:** Default **Global Mode** untuk semua aplikasi Windows, disertai pilihan **Window Filter** berbasis daftar jendela aktif dan **Typing Pause** opsional.
- **Keamanan & Transparansi Lokal (`SecurityAuditDialog`, `build.bat`, `tests/run_tests.bat`):** Nol ketergantungan eksternal, nol API jaringan, dan 10 pengujian otomatis (*unit*, *integration*, serta audit keamanan source code).
