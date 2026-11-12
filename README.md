<div align="center">

# Key-Binder

### Aplikasi KeyBinder & Auto-Sequence Macro Universal untuk Windows.

**100% offline, ukuran binary ~145 KB, nol dependensi eksternal, dan mendukung injeksi Hardware Scan-Code untuk game 3D.**

[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](https://github.com/Asharuu/Key-Binder)
[![Runtime](https://img.shields.io/badge/runtime-.NET%20Framework%204.x%20Native-512BD4?style=flat-square)](build.bat)
[![Dependencies](https://img.shields.io/badge/dependencies-0%20(Zero)-16A34A?style=flat-square)](src/Native/Win32Native.cs)
[![Network](https://img.shields.io/badge/network%20API-none%20(100%25%20offline)-0D9488?style=flat-square)](tests/SafeKeyBinderTests.cs)
[![Tests](https://img.shields.io/badge/tests-14%20passed-2563EB?style=flat-square)](tests/run_tests.bat)
[![License](https://img.shields.io/badge/license-MIT-64748B?style=flat-square)](LICENSE)

**[Fitur Utama](#fitur-utama) · [Tutorial Penggunaan](#tutorial--cara-penggunaan) · [Shortcut](#shortcut--kontrol-cepat) · [Arsitektur](#arsitektur--performa) · [Audit Keamanan](#audit-keamanan--verifikasi-mandiri) · [FAQ](#faq)**

<code>build.bat && SafeKeyBinder.exe</code>

</div>

---

## Fitur Utama

**Key-Binder** (`SafeKeyBinder`) dirancang untuk pengguna Windows dan pemain game (seperti Roblox/Growblox, GTA SAMP, maupun aplikasi produktivitas) yang membutuhkan otomasi ketikan tanpa risiko keamanan dari file `.exe` pihak ketiga yang tertutup.

| Modul / Kapabilitas | Spesifikasi Teknis & Perilaku |
| :--- | :--- |
| **Manual KeyBinder** | Memetakan tombol tunggal (`F1`–`F12`, huruf, angka, Numpad) atau kombinasi modifier (`Ctrl`, `Alt`, `Shift`) ke teks macro satu baris maupun multi-baris. |
| **Auto-Sequence Generator** | Menghasilkan dan mengeksekusi antrean berurutan secara otomatis: **Rentang Huruf** (`77a`–`77z`, `aa`–`zz`), **Rentang Angka** (`001`–`100`), atau **Daftar Manual** dengan template `{item}`. |
| **Hardware Scan-Code Injection** | Mengirim tombol `Prefix` (seperti `/`) dan `Suffix` (`Enter`, `Tab`, `Space`) menggunakan flag `KEYEVENTF_SCANCODE` (`0x0008`) dengan durasi tahan `35 ms` agar terdeteksi oleh engine DirectInput/3D. |
| **Zero Clipboard Hijack** | Mengirim karakter menggunakan paket `KEYEVENTF_UNICODE` via Win32 `SendInput`. Isi *copy-paste* (Clipboard) Windows Anda tidak pernah disentuh atau ditimpa. |
| **Live Queue Control** | Mendukung **Pause / Resume** instan dari dalam game (`F6`), memilih ulang posisi urutan saat status sedang *Pause*, serta pengaman fokus jendela aplikasi target. |
| **100% Offline & Auditable** | Dikompilasi langsung menggunakan `csc.exe` bawaan Windows. Tidak memuat library NuGet eksternal dan tidak memiliki kode jaringan (`System.Net`, `Socket`, `HttpClient`). |

---

## Tutorial & Cara Penggunaan

### 1. Instalasi & Membuka Aplikasi

Aplikasi ini tidak memerlukan installer tambahan karena menggunakan `.NET Framework 4.x` yang sudah terpasang secara bawaan di Windows 10 dan Windows 11.

1. Unduh atau *clone* repositori ini:
   ```powershell
   git clone https://github.com/Asharuu/Key-Binder.git
   cd Key-Binder
   ```
2. Jalankan langsung file **[SafeKeyBinder.exe](SafeKeyBinder.exe)**, atau kompilasi ulang dari kode sumber dengan menjalankan **[build.bat](build.bat)**.
3. Klik tombol hijau **Aktifkan KeyBinder** di pojok kanan atas jendela utama agar status berubah menjadi **`AKTIF`**.

---

### 2. Skenario Dasar: Membuat Keybind Chat / Macro Manual

Gunakan tab **KeyBinder (Hotkey Manual)** untuk mengirim teks cepat saat satu tombol ditekan (contoh: menekan tombol `5` untuk mengetik `/warp ` di dalam game):

1. Klik tombol **`+`** di atas tabel daftar keybind untuk membuka mode **Tambah Keybind Baru**.
2. Isi kolom **Label** (contoh: `Teleport Cepat`).
3. Klik tombol **Rekam** di samping kolom **Tombol**, tekan tombol pemicu yang diinginkan (misal `5` atau `F1`), lalu klik **Gunakan Tombol Ini**.
4. Ketik isi teks pada kolom **Teks / Isi Macro** (contoh: `warp 77a` atau `/warp `).
5. Atur pembuka dan akhiran perintah:
   - **Prefix:** Klik **Prefix...** lalu tekan `/` jika game membutuhkan tombol `/` fisik untuk membuka kotak chat terlebih dahulu.
   - **Suffix:** Pilih **`Enter`** jika teks ingin langsung dikirim, atau **`Tidak ada`** jika Anda ingin menambahkan huruf/angka sendiri sebelum menekan Enter.
6. Klik tombol biru **Simpan**. Centang baris keybind di tabel kiri untuk mengaktifkannya.

---

### 3. Skenario Lanjutan: Otomasi Urutan (Auto-Sequence / World Hunting)

Gunakan tab **Auto Sequence (Urutan Otomatis)** ketika Anda ingin mencoba puluhan nama *world* atau kode secara berurutan (contoh: `/warp 66a`, `/warp 66b`, hingga `/warp 66z`) secara otomatis:

1. Buka tab **Auto Sequence (Urutan Otomatis)**.
2. Pada panel **Generator Pola Urutan**, pilih salah satu metode:
   - **Huruf (77a - 77z):** Isi **Awalan** = `66`, **Huruf Dari** = `a`, **Sampai** = `z`, **Akhiran** = *(kosongkan)*.
   - **Angka (1 - 100):** Isi angka awal, angka akhir, dan centang **Zero-Pad** jika membutuhkan format `001`, `002`.
   - **Daftar Manual:** Ketik atau tempel daftar target satu per baris.
3. Klik tombol **Generate & Muat ke Antrean**. Daftar target (`1. 66a`, `2. 66b`, dst.) akan muncul di tabel **Antrean**.
4. Pada panel **Pengaturan Eksekusi & Timing**, sesuaikan parameter berikut untuk game seperti Roblox/Growblox:
   - **Format Perintah (`{item}`):** Isi `warp {item}` (atau `/warp {item}` jika chat game tidak otomatis menyertakan `/`).
   - **Prefix (Pembuka):** Klik **Prefix...** lalu rekam tombol `/`.
   - **Jeda Prefix (ms):** Isi `120` (memberi waktu `120 ms` bagi animasi kotak chat game untuk terbuka penuh sebelum teks diketik).
   - **Suffix (Akhiran):** Pilih `Enter`.
   - **Delay Huruf (ms):** Isi `10` (jeda antar karakter agar tidak ada huruf yang terlewat).
   - **Jeda Antar Item (ms):** Isi `3500` (`3,5 detik` jeda tunggu sebelum berpindah ke item berikutnya).
   - **Countdown (dtk):** Isi `3` (waktu hitung mundur agar Anda sempat berpindah ke jendela game).
5. Klik tombol **Mulai Urutan**, lalu langsung klik ke dalam jendela game Anda sebelum hitung mundur selesai.
6. **Mengontrol Urutan Saat Berjalan:**
   - Tekan **`F6`** kapan saja di dalam game untuk **Pause (Jeda)** ketika menemukan *world* yang menarik, dan tekan **`F6`** lagi untuk **Resume (Lanjut)** ke urutan berikutnya.
   - Saat berstatus **DIJEDA (PAUSE)**, Anda dapat mengklik baris mana pun di daftar **Antrean** untuk melompat atau mengulang dari item tersebut, lalu klik **Lanjutkan**.
   - Tekan **`F7`** untuk menghentikan urutan sepenuhnya (**Stop**).

---

### 4. Failsafe, Filter Jendela & Hak Akses Administrator

- **Membatasi Hanya pada Aplikasi Tertentu (Window Filter):**
  - Pada tab pertama, pilih **Hanya Aplikasi Tertentu**, klik **Refresh**, lalu pilih nama proses/jendela (misal `Roblox`).
  - Pada tab **Auto Sequence**, centang **Hanya ketik jika jendela fokus** dan pilih target jendela agar ketikan otomatis langsung tertahan (*menunggu fokus*) apabila Anda tidak sengaja berpindah ke aplikasi lain (`Alt + Tab`).
- **Hak Akses Administrator (UIPI Windows):**
  - Jika game target berjalan sebagai *Administrator*, Windows memblokir simulasi input dari proses biasa. Klik tombol **Buka sbg Admin** di pojok kanan atas aplikasi untuk memulai ulang Key-Binder dengan izin Administrator.

---

## Shortcut & Kontrol Cepat

| Hotkey Default | Dapat Diubah? | Fungsi & Perilaku |
| :--- | :---: | :--- |
| **`Pause`** (*Break*) | Ya | **Master Toggle:** Menjeda atau mengaktifkan kembali seluruh keybind manual secara global. |
| **`F6`** | Ya | **Auto-Sequence Pause / Resume:** Memulai, menjeda instan, atau melanjutkan urutan otomatis dari dalam game. |
| **`F7`** | Ya | **Auto-Sequence Stop:** Menghentikan eksekusi urutan otomatis dengan segera. |
| **`T`** *(Opsional)* | Ya | **Typing Pause:** Jika fitur *Pause saat mengetik* diaktifkan, menekan tombol ini akan menonaktifkan hotkey sementara hingga Anda menekan `Enter` atau `Esc`. |
| **`Esc`** *(di Dialog Rekam)* | Tetap | Membatalkan perekaman tombol pada dialog *Key Capture*. |

---

## Arsitektur & Performa

Aplikasi ini dibangun dengan prinsip pemisahan tanggung jawab (*Separation of Concerns*) dan efisiensi resource:

1. **Low-Level Hook Non-Blocking (`WH_KEYBOARD_LL`):**
   Callback hook pada [src/Services/KeyboardHookService.cs](src/Services/KeyboardHookService.cs) hanya memeriksa pencocokan tombol di memori RAM dalam waktu kurang dari `1 ms` dan mengabaikan seluruh event sintetik (`LLKHF_INJECTED`) untuk mencegah *infinite loop*. Pengiriman teks dijalankan pada *background worker thread* terpisah sehingga Windows tidak pernah memutus hook akibat *timeout*.
2. **Injeksi Scan-Code Fisik (`KEYEVENTF_SCANCODE`):**
   Banyak engine game 3D membaca input langsung melalui *hardware scan code* dan mengabaikan event virtual key sesaat (`0 ms`). Pada [src/Services/InputSenderService.cs](src/Services/InputSenderService.cs), tombol `Prefix` dan `Suffix` diterjemahkan melalui `MapVirtualKey(vk, MAPVK_VK_TO_VSC)` dengan flag `KEYEVENTF_SCANCODE (0x0008)` dan ditahan selama `35 ms` (~2 frame pada 60 FPS) sebelum dilepas.
3. **Pelepasan Modifier Fisik Sementara:**
   Apabila pengguna memicu hotkey kombinasi seperti `Ctrl + F9`, sistem secara otomatis mengirim sinyal lepas (`KEYEVENTF_KEYUP`) untuk `Ctrl`/`Alt`/`Shift` sebelum mengetik karakter pertama agar aplikasi target tidak salah menafsirkan huruf macro sebagai pintasan keyboard (`Ctrl + huruf`).
4. **Interruptible Sleep Slicing (`50 ms`):**
   Pada [src/Services/AutoSequenceService.cs](src/Services/AutoSequenceService.cs), jeda tunggu panjang (misalnya `3500 ms` antar *world*) dipecah menjadi potongan tidur `50 ms` yang terus memeriksa sinyal pembatalan dan *pause*. Hal ini membuat penekanan tombol `F6` atau `F7` merespons secara instan tanpa harus menunggu jeda selesai.

```text
Key-Binder/
├── assets/
│   └── app.ico                      # Ikon kustom multi-resolusi (16x16 - 256x256)
├── src/
│   ├── Program.cs                   # Entry point & konfigurasi High-DPI awareness
│   ├── Models/                      # Struktur data konfigurasi XML & item keybind
│   ├── Native/
│   │   └── Win32Native.cs           # Satu-satunya lokasi deklarasi P/Invoke Win32
│   ├── Services/                    # Engine hook, matcher, sequence generator, & input sender
│   └── UI/                          # Komponen antarmuka Windows Forms DPI-aware
├── tests/
│   ├── run_tests.bat                # Skrip eksekusi pengujian otomatis
│   └── SafeKeyBinderTests.cs        # 14 unit, integration, & source security tests
├── build.bat                        # Skrip kompilasi lokal via csc.exe bawaan Windows
├── BLUEPRINT.md                     # Dokumentasi arsitektur teknis
└── CHANGELOG.md                     # Riwayat perubahan versi
```

---

## Audit Keamanan & Verifikasi Mandiri

Anda tidak perlu memercayai file `.exe` secara buta. Seluruh kode sumber dapat diperiksa dan dikompilasi ulang sendiri dalam waktu kurang dari 5 detik:

1. **Periksa Panggilan Sistem:** Buka [src/Native/Win32Native.cs](src/Native/Win32Native.cs) — hanya terdapat fungsi standar `user32.dll` dan `kernel32.dll` untuk keyboard hook, `SendInput`, dan daftar jendela aktif.
2. **Jalankan Pengujian Otomatis & Pemindaian Keamanan:**
   ```cmd
   tests\run_tests.bat
   ```
   Skrip ini akan mengompilasi kode pengujian dan menjalankan **14 verifikasi otomatis**, termasuk memindai seluruh file `.cs` di folder `src/` untuk memastikan tidak ada penggunaan `System.Net`, `HttpClient`, `WebClient`, `WebRequest`, `Socket`, maupun `TcpClient`.
3. **Kompilasi Ulang Binary `.exe`:**
   ```cmd
   build.bat
   ```

---

## FAQ

**Q: Mengapa saat menggunakan Prefix `/` di Roblox/Growblox, perintah terkirim menjadi `//warp`?**
> **A:** Beberapa game otomatis mengetikkan karakter `/` ke dalam kotak chat saat Anda menekan tombol `/` untuk membuka chat. Jika hal itu terjadi, cukup ubah kolom **Format Perintah** dari `/warp {item}` menjadi `warp {item}` (tanpa garis miring di depan) sambil tetap membiarkan **Prefix** = `/`.

**Q: Apakah aplikasi ini perlu dijalankan bersama code editor / terminal?**
> **A:** Tidak. File **[SafeKeyBinder.exe](SafeKeyBinder.exe)** adalah aplikasi native Windows mandiri (`~145 KB`) dengan penggunaan CPU `0%` saat *standby*. Anda dapat membuat shortcut ke Desktop dan menutup seluruh editor saat bermain game.

**Q: Bagaimana jika saya ingin mulai Auto-Sequence dari urutan ke-10, bukan dari #1?**
> **A:** Klik baris ke-10 pada daftar **Antrean** sebelum menekan **Mulai Urutan** (atau saat status sedang **DIJEDA / PAUSE**), lalu tekan **Mulai Urutan** / **Lanjutkan**. Label di atas tabel akan menampilkan `Mulai #10`.

---

<p align="center"><em>“Kode yang aman tidak meminta kepercayaan buta — ia membuka dirinya untuk dibaca, diuji, dan dikompilasi ulang kapan saja.”</em></p>
<hr>

## License

MIT: [LICENSE](LICENSE)
