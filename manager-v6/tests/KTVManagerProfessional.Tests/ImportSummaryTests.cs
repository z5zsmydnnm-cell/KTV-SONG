using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Tests;

public sealed class ImportSummaryTests
{
    [Fact]
    public void FromResults_calculates_success_rate_from_imported_updated_and_duplicates()
    {
        var result = new ImportFileResult(
            SourcePath: @"D:\in\1326.pdf",
            SourceFileName: "1326.pdf",
            SourceType: ImportSourceType.Pdf,
            BrandCode: "音圓",
            VolumeCode: "1326",
            TotalRows: 10,
            ImportedRows: 4,
            UpdatedRows: 2,
            DuplicateRows: 1,
            FailedRows: 3,
            Issues: []);

        var summary = ImportSummary.FromResults([result]);

        Assert.Equal(1, summary.TotalFiles);
        Assert.Equal(10, summary.TotalRows);
        Assert.Equal(7, summary.SuccessfulRows);
        Assert.Equal(3, summary.FailedRows);
        Assert.Equal(70.0, summary.SuccessRate);
    }
}
