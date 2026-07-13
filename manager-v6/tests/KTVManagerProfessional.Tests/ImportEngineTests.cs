using KTVManagerProfessional.Core.Data;
using KTVManagerProfessional.Core.Importing;
using KTVManagerProfessional.Core.Ocr;
using iText.Kernel.Pdf;

namespace KTVManagerProfessional.Tests;

public sealed class ImportEngineTests
{
    [Fact]
    public async Task ImportFilesAsync_imports_csv_and_reports_unsupported_file()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktv-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var databasePath = Path.Combine(directory, "ktv.sqlite");
        var csvPath = Path.Combine(directory, "音圓1356.csv");
        var txtPath = Path.Combine(directory, "readme.txt");
        await File.WriteAllTextAsync(csvPath, "SongNumber,Title,Artist,Language,Volume\r\n123456,想你的夜,關喆,國語,1356\r\n");
        await File.WriteAllTextAsync(txtPath, "not a song file");
        KtvDatabase.Initialize(databasePath);

        var summary = await new ImportEngine().ImportFilesAsync([csvPath, txtPath], databasePath, CancellationToken.None);

        Assert.Equal(2, summary.TotalFiles);
        Assert.Equal(1, summary.TotalRows);
        Assert.Equal(1, summary.ImportedRows);
        Assert.Equal(1, summary.Results.Count(result => result.SourceType == ImportSourceType.Unsupported));
        Assert.Equal(1, new SongRepository(databasePath).CountSongs());
    }

    [Fact]
    public async Task ImportFilesAsync_uses_ocr_for_golden_voice_pdf_without_text_layer()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktv-import-ocr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var databasePath = Path.Combine(directory, "ktv.sqlite");
        var pdfPath = Path.Combine(directory, "goldenvoice1.pdf");
        CreateBlankPdf(pdfPath);
        var ocr = new FakePdfOcrPageExtractor([
            new OcrPage(
                PageNumber: 1,
                Width: 1654,
                Height: 2339,
                Words:
                [
                    Word("國", 1381, 92), Word("語", 1431, 93), Word("歌", 1486, 90), Word("曲", 1548, 91),
                    Word("37195", 164, 215), Word("雨", 321, 221), Word("中", 380, 215), Word("的", 431, 217), Word("思", 486, 218), Word("念", 540, 215),
                    Word("祁", 726, 227), Word("隆", 752, 227)
                ])
        ]);

        var summary = await new ImportEngine(ocr).ImportFilesAsync([pdfPath], databasePath, CancellationToken.None);

        Assert.Equal(1, summary.ImportedRows);
        Assert.Equal(0, summary.FailedRows);
        Assert.Equal(1, new SongRepository(databasePath).CountSongs());
    }

    private static void CreateBlankPdf(string path)
    {
        using var writer = new PdfWriter(path);
        using var document = new PdfDocument(writer);
        document.AddNewPage();
    }

    private static OcrWord Word(string text, double x, double y, double width = 45, double height = 45)
    {
        return new OcrWord(text, x, y, width, height);
    }

    private sealed class FakePdfOcrPageExtractor(IReadOnlyList<OcrPage> pages) : IPdfOcrPageExtractor
    {
        public bool IsAvailable => true;

        public string AvailabilityMessage => "Fake OCR is available.";

        public Task<IReadOnlyList<OcrPage>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken)
        {
            return Task.FromResult(pages);
        }
    }
}
