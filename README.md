# Key-Binder (SafeKeyBinder) — Universal Windows KeyBinder & Auto-Sequence Macro

Aplikasi **KeyBinder & Auto-Sequence Typer Universal** untuk Windows yang dibuat dengan C# Native (.NET Framework WinForms), **super ringan (~145 KB)**, **100% offline**, **nol ketergantungan eksternal (zero dependencies)**, mendukung **Hardware Scan-Code (`KEYEVENTF_SCANCODE`)** untuk game 3D (Roblox/Growblox, SAMP, dll.), serta mudah diaudit.

---

## Fitur Utama

1. **KeyBinder (Hotkey Manual):**
   - Perekam hotkey interaktif (`F1`–`F12`, huruf/angka, Numpad, kombinasi `Ctrl` / `Alt` / `Shift`).
   - Dukungan **Prefix Key** (misal `/` untuk membuka chat game) dan **Suffix Key** (`Enter`, `Tab`, `Space`) berbasis *Hardware Scan-Code* (`35 ms` key hold) + pengiriman teks Unicode anti-*clipboard hijack*.
   - **Master Toggle Hotkey** (default: `Pause`), **Window Filter** (Global atau khusus aplikasi tertentu), dan **Typing Pause** opsional.
2. **Auto Sequence (Urutan Otomatis):**
   - Generator antrean otomatis untuk **Rentang Huruf** (`77a`–`77z`, `aa`–`zz`), **Rentang Angka** (`1`–`100` dengan *zero-padding*), maupun **Daftar Manual**.
   - Template perintah fleksibel (`warp {item}` atau `/warp {item}`), pengaturan jeda *Prefix*, *Delay Huruf*, *Jeda Antar Item*, *Countdown*, serta opsi *Loop*.
   - **Hotkey Cepat Global:** `F6` untuk **Pause / Resume** instan di tengah jalan (bisa pilih ulang posisi antrean saat *Pause*) dan `F7` untuk **Stop**.
3. **100% Offline & Transparan:**
   - Tidak mengimpor modul jaringan (`System.Net`, `HttpClient`, `Socket`, dll.) dan tidak pernah merekam ketikan ke file.

---

## Cara Menggunakan

1. Jalankan `SafeKeyBinder.exe` di folder ini.
2. Pada tab **KeyBinder (Hotkey Manual)**:
   - Klik **Aktifkan KeyBinder** (kanan atas).
   - Klik **+ Tambah**, rekam tombol pemicu, isi teks macro, atur **Prefix / Suffix** bila perlu, lalu klik **Simpan**.
3. Pada tab **Auto Sequence (Urutan Otomatis)**:
   - Pilih pola urutan (**Huruf**, **Angka**, atau **Daftar Manual**) lalu klik **Generate & Muat ke Antrean**.
   - Atur format perintah (contoh: `warp {item}` dengan `Prefix = /` dan `Suffix = Enter`), lalu klik **Mulai Urutan** (atau tekan **`F6`**).

---

## Cara Verifikasi Keamanan & Compile Ulang Sendiri

Jika Anda ingin memverifikasi sendiri bahwa file `.exe` benar-benar berasal dari source code di folder `src\`:

1. Buka dan baca file di folder `src\` (terutama `src\Native\Win32Native.cs` dan `src\Services\KeyboardHookService.cs`).
2. Pastikan tidak ada pemanggilan API jaringan (`System.Net`, `HttpClient`, `WebClient`, `Socket`, dll.) dan tidak ada pencatatan tombol ke file.
3. Jalankan `build.bat` untuk mengompilasi ulang `SafeKeyBinder.exe` secara langsung menggunakan *compiler* bawaan Windows (`csc.exe`).
4. Jalankan `tests\run_tests.bat` untuk menjalankan 14 pengujian otomatis (termasuk pemindaian keamanan source code).

---

## Catatan Hak Akses Windows (Administrator)

Jika Anda menggunakan keybinder pada aplikasi atau game yang dijalankan sebagai **Administrator**, Windows membatasi input dari aplikasi non-Admin demi keamanan (aturan UIPI). Gunakan tombol **Buka sbg Admin** di pojok kanan atas aplikasi bila diperlukan.
