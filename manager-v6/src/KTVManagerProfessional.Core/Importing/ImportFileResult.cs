namespace KTVManagerProfessional.Core.Importing;

public sealed record ImportFileResult(
    string SourcePath,
    string SourceFileName,
    ImportSourceType SourceType,
    string BrandCode,
    string VolumeCode,
    int TotalRows,
    int ImportedRows,
    int UpdatedRows,
    int DuplicateRows,
    int FailedRows,
    IReadOnlyList<ParseIssue> Issues);
