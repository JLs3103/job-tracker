# Personal IT Job Tracker

## Jalankan aplikasi

Prasyarat: .NET 10 SDK, Node.js, dan npm.

- **Windows:** jalankan `run.bat`, atau di PowerShell gunakan `.\run.ps1`.
- **Linux/macOS:** jalankan `bash ./run.sh`.

Launcher membangun backend, memasang dependensi frontend jika belum tersedia, lalu menjalankan keduanya dalam mode integrasi. Migrasi SQLite diterapkan otomatis saat backend mulai. Tekan `Ctrl+C` untuk menghentikan layanan.

- Aplikasi: http://localhost:5173
- Backend API: http://localhost:5227
- OpenAPI: http://localhost:5227/openapi/v1.json

Kunci Gemini bersifat opsional. Tanpa kunci atau jika layanan AI gagal, backend tetap menyimpan lowongan dengan data ekstraksi fallback untuk ditinjau.
