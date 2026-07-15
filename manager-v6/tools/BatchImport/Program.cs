using KTVManagerProfessional.Core;
using KTVManagerProfessional.Core.Data;
using KTVManagerProfessional.Core.Importing;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: BatchImport <pdf-directory> [seed-csv-or-excel ...]");
    return 2;
}

var sourceDirectory = Path.GetFullPath(args[0]);
if (!Directory.Exists(sourceDirectory))
{
    Console.Error.WriteLine($"PDF directory does not exist: {sourceDirectory}");
    return 2;
}

var repositoryPath = GitRepositorySettings.DefaultRepositoryPath;
var databasePath = Path.Combine(repositoryPath, "manager-v6", "data", "ktv-manager-v6.sqlite");
var songsDirectoryPath = SongLibraryPaths.DefaultSongsDirectoryPath;
var masterCsvPath = SongLibraryPaths.DefaultMasterCsvPath;

var pdfPaths = Directory
    .EnumerateFiles(sourceDirectory, "*.pdf", SearchOption.TopDirectoryOnly)
    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
    .ToList();
var seedPaths = args
    .Skip(1)
    .Select(Path.GetFullPath)
    .Where(File.Exists)
    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
    .ToList();

var importPaths = new List<string>();
importPaths.AddRange(seedPaths);
if (File.Exists(masterCsvPath))
{
    importPaths.Add(masterCsvPath);
}

importPaths.AddRange(pdfPaths);

Console.WriteLine($"Repository: {repositoryPath}");
Console.WriteLine($"Database: {databasePath}");
Console.WriteLine($"Source PDFs: {pdfPaths.Count}");
Console.WriteLine($"Seed files: {seedPaths.Count}");
Console.WriteLine($"Import files: {importPaths.Count}");

var summary = await new ImportEngine().ImportFilesAsync(importPaths, databasePath, CancellationToken.None);

var repository = new SongRepository(databasePath);
var songs = repository.GetAllSongs();
CsvExporter.ExportMasterCsv(masterCsvPath, songs);
CsvExporter.ExportBrandCsvs(songsDirectoryPath, songs);

Console.WriteLine(
    $"Summary: new={summary.ImportedRows}, updated={summary.UpdatedRows}, duplicate={summary.DuplicateRows}, failed={summary.FailedRows}, successRate={summary.SuccessRate:0.0}%");
Console.WriteLine($"Database songs: {repository.CountSongs()}");
Console.WriteLine($"Exported song records: {songs.Count}");
Console.WriteLine();
Console.WriteLine("Top failed files:");

foreach (var result in summary.Results.OrderByDescending(result => result.FailedRows).ThenBy(result => result.SourceFileName).Take(30))
{
    Console.WriteLine(
        $"{result.SourceFileName}: brand={result.BrandCode}, volume={result.VolumeCode}, total={result.TotalRows}, new={result.ImportedRows}, updated={result.UpdatedRows}, duplicate={result.DuplicateRows}, failed={result.FailedRows}");
}

return 0;
