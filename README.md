# Personal IT Job Tracker

## Jalankan aplikasi

Prasyarat: .NET 10 SDK, Node.js, dan npm.

- **Windows:** jalankan `run.bat`, atau di PowerShell gunakan `.\run.ps1`.
- **Linux/macOS:** jalankan `bash ./run.sh`.

Launcher membangun backend, memasang dependensi frontend jika belum tersedia, lalu menjalankan keduanya dalam mode integrasi. Migrasi SQLite diterapkan otomatis saat backend mulai. Tekan `Ctrl+C` untuk menghentikan layanan.

- Aplikasi: http://localhost:5173
- Backend API: http://localhost:5227
- OpenAPI: http://localhost:5227/openapi/v1.json

Ekstraksi mencoba model lokal Ollama `qwen3:4b` terlebih dahulu, lalu menggunakan Gemini jika Ollama tidak tersedia atau hasilnya tidak valid. Siapkan Ollama dan jalankan `ollama pull qwen3:4b`. Gemini API key tetap opsional; jika kedua provider gagal, backend menyimpan lowongan dengan data ekstraksi fallback untuk ditinjau.

Jika ekstraksi gagal, atur Gemini API key dan model yang tersedia, lalu pilih **Coba ekstrak ulang** pada kartu lowongan. Proses ini mengambil ulang feed sumber; jika artikelnya sudah tidak ada di feed, gunakan tautan sumber pada kartu.

Refresh pertama memeriksa postingan dalam 7 hari terakhir. Setelah refresh sukses, pemeriksaan berikutnya dimulai dari waktu pemeriksaan sukses terakhir, dengan batas lookback maksimum 7 hari. Lowongan disimpan saat ekstraksi tiap artikel selesai, jadi hasil yang sudah diproses tidak hilang jika refresh terputus; checkpoint baru maju setelah semua sumber berhasil diperiksa. Jika ada sumber yang gagal, checkpoint tidak maju agar postingan dari sumber itu tidak terlewat. Data yang sudah tersimpan lebih lama tidak dihapus otomatis. Daftar menampilkan 20 lowongan per halaman. Klik biasa maupun klik tengah pada **Lihat Lowongan** menandai lowongan baru sebagai **Dilihat**.
