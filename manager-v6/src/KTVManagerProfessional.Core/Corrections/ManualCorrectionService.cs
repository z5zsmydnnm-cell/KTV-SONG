using KTVManagerProfessional.Core.Data;

namespace KTVManagerProfessional.Core.Corrections;

public sealed class ManualCorrectionService
{
    private readonly string _databasePath;

    public ManualCorrectionService(string databasePath)
    {
        _databasePath = databasePath;
    }

    public void AcceptCorrection(ManualCorrection correction)
    {
        AddCorrection(correction, "Accepted");
        if (string.IsNullOrWhiteSpace(correction.SongNumber) || string.IsNullOrWhiteSpace(correction.Title) || string.IsNullOrWhiteSpace(correction.BrandCode))
        {
            return;
        }

        var song = new SongRecord(
            correction.SongNumber,
            correction.Title,
            correction.ArtistName ?? string.Empty,
            correction.Language ?? "Unknown",
            correction.BrandCode,
            correction.VolumeCode ?? string.Empty);

        new SongRepository(_databasePath).UpsertSong(song, "manual-correction", DateTimeOffset.Now);
    }

    public void IgnoreCorrection(ManualCorrection correction)
    {
        AddCorrection(correction, "Ignored");
    }

    private void AddCorrection(ManualCorrection correction, string status)
    {
        using var connection = KtvDatabase.OpenConnection(_databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ManualCorrections (
                ImportHistoryId, PageNumber, RawText, OcrText, SongNumber, Title, ArtistName,
                Language, BrandCode, VolumeCode, Status, CreatedAt, UpdatedAt)
            VALUES (
                $importHistoryId, $pageNumber, $rawText, $ocrText, $songNumber, $title, $artistName,
                $language, $brandCode, $volumeCode, $status, $createdAt, $updatedAt)
            """;
        command.Parameters.AddWithValue("$importHistoryId", correction.ImportHistoryId);
        command.Parameters.AddWithValue("$pageNumber", correction.PageNumber);
        command.Parameters.AddWithValue("$rawText", correction.RawText);
        command.Parameters.AddWithValue("$ocrText", (object?)correction.OcrText ?? DBNull.Value);
        command.Parameters.AddWithValue("$songNumber", (object?)correction.SongNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("$title", (object?)correction.Title ?? DBNull.Value);
        command.Parameters.AddWithValue("$artistName", (object?)correction.ArtistName ?? DBNull.Value);
        command.Parameters.AddWithValue("$language", (object?)correction.Language ?? DBNull.Value);
        command.Parameters.AddWithValue("$brandCode", (object?)correction.BrandCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$volumeCode", (object?)correction.VolumeCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$createdAt", DateTimeOffset.Now.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.Now.ToString("O"));
        command.ExecuteNonQuery();
    }
}
