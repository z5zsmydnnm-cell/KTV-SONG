using KTVManagerProfessional.Core.Data;
using KTVManagerProfessional.Core.Parsing;

namespace KTVManagerProfessional.Core.Importing;

public sealed class ImportEngine
{
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

    private static Task<ImportFileResult> ImportFileAsync(string path, string databasePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var route = ImportRouter.Route(path);
        if (route.IsUnsupported)
        {
            return Task.FromResult(new ImportFileResult(
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
                Issues: [new ParseIssue(0, Path.GetFileName(path), route.Reason)]));
        }

        var parseResult = Parse(path, route);
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
        return Task.FromResult(new ImportFileResult(
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
            Issues: parseResult.Issues));
    }

    private static ParseResult Parse(string path, ImportRoute route)
    {
        return route.SourceType switch
        {
            ImportSourceType.Csv => new CsvSongParser().ParseFile(path, route.BrandCode),
            ImportSourceType.Excel => new ExcelSongParser().ParseFile(path, route.BrandCode),
            ImportSourceType.Pdf => ParsePdf(path, route.BrandCode),
            _ => new ParseResult([], [new ParseIssue(0, Path.GetFileName(path), "Unsupported source type.")])
        };
    }

    private static ParseResult ParsePdf(string path, string brandCode)
    {
        var text = PdfTextExtractor.ExtractText(path);
        ISongParser parser = brandCode switch
        {
            BrandCode.GoldenVoice => new GoldenVoicePdfSongParser(),
            _ => new InYuanPdfSongParser()
        };
        return parser.Parse(text, Path.GetFileName(path));
    }
}
