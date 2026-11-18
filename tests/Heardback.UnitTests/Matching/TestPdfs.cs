using System.Globalization;
using System.IO.Compression;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Heardback.UnitTests.Matching;

/// <summary>Small PDFs built in memory for the CV reader's tests.</summary>
internal static class TestPdfs
{
    private const double FontSize = 11;
    private const double Left = 50;
    private const double Top = 780;
    private const double LineHeight = 16;

    /// <summary>A PDF with one page per entry; each page shows its lines top to bottom.</summary>
    public static byte[] WithPages(params string[][] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (var lines in pages)
        {
            var page = builder.AddPage(PageSize.A4);
            for (var index = 0; index < lines.Length; index++)
            {
                page.AddText(lines[index], FontSize, new PdfPoint(Left, Top - (index * LineHeight)), font);
            }
        }

        return builder.Build();
    }

    public static byte[] WithLines(params string[] lines) => WithPages(lines);

    /// <summary>A one-page PDF whose content stream is Flate-compressed and unpacks to <paramref name="size"/> bytes.</summary>
    public static byte[] WithFlateContentOf(int size)
    {
        var content = new byte[size];
        Array.Fill(content, (byte)' ');
        Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 712 Td (Hello) Tj ET").CopyTo(content, 0);
        return Handmade(Deflate(content), "/FlateDecode");
    }

    /// <summary>A one-page PDF whose content stream claims LZW compression.</summary>
    public static byte[] WithLzwContent() => Handmade([0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01], "/LZWDecode");

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(data);
        }

        return output.ToArray();
    }

    // A minimal PDF written by hand, so tests can use streams no PDF writer would produce.
    private static byte[] Handmade(byte[] stream, string filter)
    {
        using var pdf = new MemoryStream();
        var offsets = new List<long>();

        void Write(string text) => pdf.Write(Encoding.ASCII.GetBytes(text));
        void Object(string body)
        {
            offsets.Add(pdf.Position);
            Write(string.Create(CultureInfo.InvariantCulture, $"{offsets.Count} 0 obj\n{body}\nendobj\n"));
        }

        Write("%PDF-1.7\n");
        Object("<< /Type /Catalog /Pages 2 0 R >>");
        Object("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        Object(
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R " +
            "/Resources << /Font << /F1 5 0 R >> >> >>");

        offsets.Add(pdf.Position);
        Write(string.Create(
            CultureInfo.InvariantCulture,
            $"4 0 obj\n<< /Length {stream.Length} /Filter {filter} >>\nstream\n"));
        pdf.Write(stream);
        Write("\nendstream\nendobj\n");

        Object("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var xref = pdf.Position;
        Write(string.Create(CultureInfo.InvariantCulture, $"xref\n0 {offsets.Count + 1}\n0000000000 65535 f \n"));
        foreach (var offset in offsets)
        {
            Write(string.Create(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n"));
        }

        Write(string.Create(
            CultureInfo.InvariantCulture,
            $"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"));
        return pdf.ToArray();
    }
}
