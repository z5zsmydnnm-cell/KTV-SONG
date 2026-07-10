using System.Text;
using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Core.Git;

public static class ReleaseNoteGenerator
{
    public static string Generate(ImportSummary summary, string exportedCsvPath, DateTimeOffset generatedAt)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# KTV Song Database Update {generatedAt:yyyy-MM-dd HH:mm}");
        builder.AppendLine();
        builder.AppendLine($"Files imported: {summary.TotalFiles}");
        builder.AppendLine($"Total rows: {summary.TotalRows}");
        builder.AppendLine($"New songs: {summary.ImportedRows}");
        builder.AppendLine($"Updated songs: {summary.UpdatedRows}");
        builder.AppendLine($"Duplicates skipped: {summary.DuplicateRows}");
        builder.AppendLine($"Failed rows: {summary.FailedRows}");
        builder.AppendLine($"Success rate: {summary.SuccessRate:0.0}%");
        builder.AppendLine($"Exported CSV: {exportedCsvPath}");
        return builder.ToString();
    }
}
