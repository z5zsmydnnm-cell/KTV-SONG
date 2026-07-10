using KTVManagerProfessional.Core.Data;
using KTVManagerProfessional.Core.Preview;

namespace KTVManagerProfessional.Tests;

public sealed class PdfImportDiagnosticsTests
{
    [Fact]
    public void AddPdfPageDiagnostic_records_page_diagnostic()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ktv-{Guid.NewGuid():N}.sqlite");
        KtvDatabase.Initialize(path);
        var repository = new PdfDiagnosticsRepository(path);

        repository.AddPageDiagnostic(new PdfPageDiagnostic(
            ImportHistoryId: 1,
            PageNumber: 2,
            TextLayerCharacterCount: 0,
            SongLikeRowCount: 0,
            ParserIssueCount: 3,
            OcrRan: false,
            OcrCharacterCount: 0,
            Confidence: 0.1,
            CreatedAt: DateTimeOffset.Parse("2026-07-10T12:00:00Z")));

        Assert.Equal(1, repository.CountPageDiagnostics());
    }
}
