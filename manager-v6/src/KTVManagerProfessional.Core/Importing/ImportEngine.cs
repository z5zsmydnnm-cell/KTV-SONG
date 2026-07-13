using KTVManagerProfessional.Core.Data;
using KTVManagerProfessional.Core.Ocr;
using KTVManagerProfessional.Core.Parsing;

namespace KTVManagerProfessional.Core.Importing;

public sealed class ImportEngine
{
    private readonly IPdfOcrPageExtractor pdfOcrPageExtractor;

    public ImportEngine()
        : this(new WindowsPdfOcrPageExtractor())
    {
    }

    public ImportEngine(IPdfOcrPageExtractor pdfOcrPageExtractor)
    {
        this.pdfOcrPageExtractor = pdfOcrPageExtractor;
    }

    public async Task<ImportSummary> ImportFilesAsync(IReadOnlyList<string> paths, string databasePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        KtvDatabase.Initialize(databasePath);
        var results = new List<ImportFileResult>();

        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ImportFileAsync(path, databasePath, cancellationToken));
        }

        return ImportSummary.FromResults(results);
    }

    private async Task<ImportFileResult> ImportFileAsync(string path, string databasePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var route = ImportRouter.Route(path);
        if (route.IsUnsupported)
        {
            return new ImportFileResult(
                SourcePath: path,
                SourceFileName: Path.GetFileName(path),
                SourceType: route.SourceType,
                BrandCode: route.BrandCode,
                VolumeCode: string.Empty,
                TotalRows: 0,
                ImportedRows: 0,
                UpdatedRows: 0,
                DuplicateRows: 0,
                FailedRows: 0,
                Issues: [new ParseIssue(0, Path.GetFileName(path), route.Reason)]);
        }

        var parseResult = await ParseAsync(path, route, cancellationToken);
        var repository = new SongRepository(databasePath);
        var imported = 0;
        var updated = 0;
        var duplicate = 0;
        var now = DateTimeOffset.Now;

        foreach (var song in parseResult.Songs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var write = repository.UpsertSong(song, Path.GetFileName(path), now);
            switch (write.Status)
            {
                case SongWriteStatus.New:
                    imported++;
                    break;
                case SongWriteStatus.Updated:
                    updated++;
                    break;
                case SongWriteStatus.Duplicate:
                    duplicate++;
                    break;
            }
        }

        var volume = parseResult.Songs.FirstOrDefault()?.Volume ?? string.Empty;
        return new ImportFileResult(
            SourcePath: path,
            SourceFileName: Path.GetFileName(path),
            SourceType: route.SourceType,
            BrandCode: route.BrandCode,
            VolumeCode: volume,
            TotalRows: parseResult.Songs.Count + parseResult.Issues.Count,
            ImportedRows: imported,
            UpdatedRows: updated,
            DuplicateRows: duplicate,
            FailedRows: parseResult.Issues.Count,
            Issues: parseResult.Issues);
    }

    private Task<ParseResult> ParseAsync(string path, ImportRoute route, CancellationToken cancellationToken)
    {
        return route.SourceType switch
        {
            ImportSourceType.Csv => Task.FromResult(new CsvSongParser().ParseFile(path, route.BrandCode)),
            ImportSourceType.Excel => Task.FromResult(new ExcelSongParser().ParseFile(path, route.BrandCode)),
            ImportSourceType.Pdf => ParsePdfAsync(path, route.BrandCode, cancellationToken),
            _ => Task.FromResult(new ParseResult([], [new ParseIssue(0, Path.GetFileName(path), "Unsupported source type.")]))
        };
    }

    private async Task<ParseResult> ParsePdfAsync(string path, string brandCode, CancellationToken cancellationToken)
    {
        var text = PdfTextExtractor.ExtractText(path);
        ISongParser parser = brandCode switch
        {
            BrandCode.GoldenVoice => new GoldenVoicePdfSongParser(),
            _ => new InYuanPdfSongParser()
        };
        var result = parser.Parse(text, Path.GetFileName(path));
        if (brandCode != BrandCode.GoldenVoice || result.Songs.Count > 0 || !string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        if (!pdfOcrPageExtractor.IsAvailable)
        {
            return result with
            {
                Issues = result.Issues
                    .Concat([new ParseIssue(0, Path.GetFileName(path), pdfOcrPageExtractor.AvailabilityMessage)])
                    .ToList()
            };
        }

        var pages = await pdfOcrPageExtractor.ExtractPagesAsync(path, cancellationToken);
        var ocrResult = new GoldenVoiceOcrSongParser().ParsePages(pages, Path.GetFileName(path));
        return ocrResult.Songs.Count > 0 ? ocrResult : result;
    }
}
