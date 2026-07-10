using KTVManagerProfessional.Core.Data;

namespace KTVManagerProfessional.Core.Preview;

public sealed class PdfDiagnosticsRepository
{
    private readonly string _databasePath;

    public PdfDiagnosticsRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public void AddPageDiagnostic(PdfPageDiagnostic diagnostic)
    {
        using var connection = KtvDatabase.OpenConnection(_databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO PdfPageDiagnostics (
                ImportHistoryId, PageNumber, TextLayerCharacterCount, SongLikeRowCount,
                ParserIssueCount, OcrRan, OcrCharacterCount, Confidence, CreatedAt)
            VALUES (
                $importHistoryId, $pageNumber, $textLayerCharacterCount, $songLikeRowCount,
                $parserIssueCount, $ocrRan, $ocrCharacterCount, $confidence, $createdAt)
            """;
        command.Parameters.AddWithValue("$importHistoryId", diagnostic.ImportHistoryId);
        command.Parameters.AddWithValue("$pageNumber", diagnostic.PageNumber);
        command.Parameters.AddWithValue("$textLayerCharacterCount", diagnostic.TextLayerCharacterCount);
        command.Parameters.AddWithValue("$songLikeRowCount", diagnostic.SongLikeRowCount);
        command.Parameters.AddWithValue("$parserIssueCount", diagnostic.ParserIssueCount);
        command.Parameters.AddWithValue("$ocrRan", diagnostic.OcrRan ? 1 : 0);
        command.Parameters.AddWithValue("$ocrCharacterCount", diagnostic.OcrCharacterCount);
        command.Parameters.AddWithValue("$confidence", diagnostic.Confidence);
        command.Parameters.AddWithValue("$createdAt", diagnostic.CreatedAt.ToString("O"));
        command.ExecuteNonQuery();
    }

    public int CountPageDiagnostics()
    {
        using var connection = KtvDatabase.OpenConnection(_databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM PdfPageDiagnostics";
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
