using System.Text;
using UglyToad.PdfPig;

namespace backend.Services;

public static class PdfTextExtractor
{
    private const int MaxExtractedCharacters = 20000;
    private static readonly byte[] PdfHeader = "%PDF-"u8.ToArray();

    public static string ExtractText(Stream pdfStream)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        using var buffer = new MemoryStream();
        pdfStream.CopyTo(buffer);
        var bytes = buffer.ToArray();

        if (bytes.Length < PdfHeader.Length ||
            !bytes.AsSpan(0, PdfHeader.Length).SequenceEqual(PdfHeader))
        {
            throw new ArgumentException("File yang diunggah bukan berkas PDF yang valid.");
        }

        using var memoryStream = new MemoryStream(bytes, writable: false);
        using var document = PdfDocument.Open(memoryStream);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            var pageText = page.Text?.Trim();
            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(pageText);
            if (builder.Length >= MaxExtractedCharacters)
            {
                break;
            }
        }

        var extracted = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(extracted))
        {
            throw new CvTextUnavailableException(
                "PDF tidak berisi teks yang dapat dibaca (kemungkinan hasil scan gambar). Gunakan PDF berbasis teks atau isi profil secara manual.");
        }

        return extracted.Length <= MaxExtractedCharacters
            ? extracted
            : extracted[..MaxExtractedCharacters];
    }
}
