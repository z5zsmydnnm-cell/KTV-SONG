using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace KTVManagerProfessional.Core;

public static class PdfTextExtractor
{
    public static string ExtractText(string pdfPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);

        using var reader = new PdfReader(pdfPath);
        using var document = new PdfDocument(reader);
        var builder = new StringBuilder();

        for (var pageNumber = 1; pageNumber <= document.GetNumberOfPages(); pageNumber++)
        {
            builder.AppendLine(iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(document.GetPage(pageNumber)));
        }

        return builder.ToString();
    }
}
