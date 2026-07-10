namespace KTVManagerProfessional.Core.Importing;

public sealed record ImportSummary(
    int TotalFiles,
    int TotalRows,
    int ImportedRows,
    int UpdatedRows,
    int DuplicateRows,
    int FailedRows,
    double SuccessRate,
    IReadOnlyList<ImportFileResult> Results)
{
    public int SuccessfulRows => ImportedRows + UpdatedRows + DuplicateRows;

    public static ImportSummary FromResults(IEnumerable<ImportFileResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var resultList = results.ToList();
        var totalRows = resultList.Sum(result => result.TotalRows);
        var importedRows = resultList.Sum(result => result.ImportedRows);
        var updatedRows = resultList.Sum(result => result.UpdatedRows);
        var duplicateRows = resultList.Sum(result => result.DuplicateRows);
        var failedRows = resultList.Sum(result => result.FailedRows);
        var successfulRows = importedRows + updatedRows + duplicateRows;
        var successRate = totalRows == 0 ? 0 : Math.Round(successfulRows * 100.0 / totalRows, 1);

        return new ImportSummary(
            TotalFiles: resultList.Count,
            TotalRows: totalRows,
            ImportedRows: importedRows,
            UpdatedRows: updatedRows,
            DuplicateRows: duplicateRows,
            FailedRows: failedRows,
            SuccessRate: successRate,
            Results: resultList);
    }
}
