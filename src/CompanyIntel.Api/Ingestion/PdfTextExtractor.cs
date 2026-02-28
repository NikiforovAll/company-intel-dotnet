using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace CompanyIntel.Api.Ingestion;

public record PdfExtractionResult(string Text, int PageCount);

public static class PdfTextExtractor
{
    public static PdfExtractionResult Extract(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var sb = new StringBuilder();

        for (var i = 0; i < document.NumberOfPages; i++)
        {
            var page = document.GetPage(i + 1);
            var text = ContentOrderTextExtractor.GetText(page);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (sb.Length > 0)
                sb.AppendLine();

            sb.AppendLine($"## Page {i + 1}");
            sb.AppendLine();
            sb.AppendLine(text.Trim());
        }

        return new PdfExtractionResult(sb.ToString(), document.NumberOfPages);
    }
}
