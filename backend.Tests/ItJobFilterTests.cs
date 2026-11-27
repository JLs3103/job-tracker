using backend.Services;

namespace backend.Tests;

public sealed class ItJobFilterTests
{
    [Fact]
    public void Rejects_OperatorProduksi_WithGeneralQualificationsAndShiftWork()
    {
        var result = ItJobFilter.IsRelevant(
            "Lowongan Kerja Operator Produksi PT Maju Jaya",
            "Kualifikasi: Pendidikan minimal SMP/SMA semua jurusan, bersedia kerja shift, mampu menginput data produksi harian.");

        Assert.False(result);
    }

    [Fact]
    public void Rejects_StaffQualityAssurance_FocusedOnSopAndOperationalQuality()
    {
        var result = ItJobFilter.IsRelevant(
            "Staff Quality Assurance - PT Manufaktur Sejahtera",
            "Tugas utama: Menyusun dan mengawasi SOP pabrik, melakukan audit operasional dan kualitas operasional sesuai standar ISO 9001 dan GMP.");

        Assert.False(result);
    }

    [Fact]
    public void Rejects_BisnisDigital_WithoutCoreTechnicalItDuties()
    {
        var result = ItJobFilter.IsRelevant(
            "Staff Bisnis Digital - PT Niaga Kreatif",
            "Tanggung jawab: Mengembangkan strategi bisnis digital, mengelola kampanye digital marketing, social media, dan laporan penjualan via web.");

        Assert.False(result);
    }

    [Fact]
    public void Rejects_NonItPosting_WhenSingleItWordOnlyAppearsInArticleBody()
    {
        var result = ItJobFilter.IsRelevant(
            "Staff Administrasi & Keuangan - PT Berkah Abadi",
            "Kualifikasi: Pendidikan S1 Akuntansi, teliti mengolah data laporan keuangan, terbiasa berkoordinasi dengan tim IT internal.");

        Assert.False(result);
    }

    [Fact]
    public void Accepts_SoftwareQaEngineer()
    {
        var result = ItJobFilter.IsRelevant(
            "Software QA Engineer - PT Teknologi Nusantara",
            "Tanggung jawab: Membuat test case, melakukan regression testing dan API testing menggunakan Postman serta Playwright.");

        Assert.True(result);
    }

    [Fact]
    public void Accepts_ItSupport()
    {
        var result = ItJobFilter.IsRelevant(
            "IT Support - PT Solusi Digital",
            "Kualifikasi: Troubleshooting hardware/software, manajemen jaringan LAN/WAN, dan dukungan helpdesk pengguna.");

        Assert.True(result);
    }

    [Fact]
    public void Accepts_CompanyOnlyTitle_WhenBodyContainsStrongItRole()
    {
        var result = ItJobFilter.IsRelevant(
            "PT Solusi Awan Indonesia",
            "Posisi yang dibutuhkan: Backend Developer. Kualifikasi: Menguasai C#, ASP.NET Core, REST API, dan PostgreSQL.");

        Assert.True(result);
    }
}
