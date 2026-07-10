using Microsoft.Data.Sqlite;

namespace KTVManagerProfessional.Core.Data;

public sealed class SongRepository
{
    private readonly string _databasePath;

    public SongRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public SongWriteResult UpsertSong(SongRecord song, string sourceFileName, DateTimeOffset importedAt)
    {
        using var connection = KtvDatabase.OpenConnection(_databasePath);
        using var transaction = connection.BeginTransaction();

        var brandId = EnsureBrand(connection, transaction, song.BrandCode, importedAt);
        var volumeId = string.IsNullOrWhiteSpace(song.Volume) ? null : EnsureVolume(connection, transaction, brandId, song.Volume, importedAt);
        var artistId = string.IsNullOrWhiteSpace(song.Artist) ? null : EnsureArtist(connection, transaction, song.Artist, importedAt);
        var existing = FindSong(connection, transaction, brandId, song.SongNumber);

        SongWriteResult result;
        if (existing is null)
        {
            var songId = InsertSong(connection, transaction, song, brandId, volumeId, artistId, sourceFileName, importedAt);
            if (artistId is not null)
            {
                UpsertSongArtist(connection, transaction, songId, artistId.Value);
            }

            result = new SongWriteResult(SongWriteStatus.New, songId);
        }
        else if (IsDuplicate(existing, song, volumeId, artistId))
        {
            result = new SongWriteResult(SongWriteStatus.Duplicate, existing.Id);
        }
        else
        {
            UpdateSong(connection, transaction, existing.Id, song, volumeId, artistId, sourceFileName, importedAt);
            if (artistId is not null)
            {
                UpsertSongArtist(connection, transaction, existing.Id, artistId.Value);
            }

            result = new SongWriteResult(SongWriteStatus.Updated, existing.Id);
        }

        transaction.Commit();
        return result;
    }

    public int CountSongs()
    {
        using var connection = KtvDatabase.OpenConnection(_databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Songs";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public IReadOnlyList<SongRecord> GetAllSongs()
    {
        using var connection = KtvDatabase.OpenConnection(_databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.SongNumber,
                   s.Title,
                   COALESCE(a.Name, ''),
                   s.Language,
                   b.Code,
                   COALESCE(v.Code, '')
            FROM Songs s
            JOIN Brands b ON b.Id = s.BrandId
            LEFT JOIN Volumes v ON v.Id = s.VolumeId
            LEFT JOIN Artists a ON a.Id = s.PrimaryArtistId
            ORDER BY b.Code, s.SongNumber
            """;
        using var reader = command.ExecuteReader();
        var songs = new List<SongRecord>();
        while (reader.Read())
        {
            songs.Add(new SongRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)));
        }

        return songs;
    }

    private static long EnsureBrand(SqliteConnection connection, SqliteTransaction transaction, string code, DateTimeOffset now)
    {
        return EnsureLookup(connection, transaction, "Brands", "Code", code, ("DisplayName", code), now);
    }

    private static long? EnsureVolume(SqliteConnection connection, SqliteTransaction transaction, long brandId, string code, DateTimeOffset now)
    {
        using var select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText = "SELECT Id FROM Volumes WHERE BrandId = $brandId AND Code = $code";
        select.Parameters.AddWithValue("$brandId", brandId);
        select.Parameters.AddWithValue("$code", code);
        var existing = select.ExecuteScalar();
        if (existing is not null)
        {
            return Convert.ToInt64(existing);
        }

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO Volumes (BrandId, Code, DisplayName, CreatedAt)
            VALUES ($brandId, $code, $displayName, $createdAt);
            SELECT last_insert_rowid();
            """;
        insert.Parameters.AddWithValue("$brandId", brandId);
        insert.Parameters.AddWithValue("$code", code);
        insert.Parameters.AddWithValue("$displayName", code);
        insert.Parameters.AddWithValue("$createdAt", now.ToString("O"));
        return Convert.ToInt64(insert.ExecuteScalar());
    }

    private static long? EnsureArtist(SqliteConnection connection, SqliteTransaction transaction, string name, DateTimeOffset now)
    {
        return EnsureLookup(connection, transaction, "Artists", "Name", name, ("NormalizedName", Normalize(name)), now);
    }

    private static long EnsureLookup(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string table,
        string keyColumn,
        string keyValue,
        (string Column, string Value) extra,
        DateTimeOffset now)
    {
        using var select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText = $"SELECT Id FROM {table} WHERE {keyColumn} = $key";
        select.Parameters.AddWithValue("$key", keyValue);
        var existing = select.ExecuteScalar();
        if (existing is not null)
        {
            return Convert.ToInt64(existing);
        }

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = $"""
            INSERT INTO {table} ({keyColumn}, {extra.Column}, CreatedAt)
            VALUES ($key, $extra, $createdAt);
            SELECT last_insert_rowid();
            """;
        insert.Parameters.AddWithValue("$key", keyValue);
        insert.Parameters.AddWithValue("$extra", extra.Value);
        insert.Parameters.AddWithValue("$createdAt", now.ToString("O"));
        return Convert.ToInt64(insert.ExecuteScalar());
    }

    private static ExistingSong? FindSong(SqliteConnection connection, SqliteTransaction transaction, long brandId, string songNumber)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT Id, Title, Language, VolumeId, PrimaryArtistId
            FROM Songs
            WHERE BrandId = $brandId AND SongNumber = $songNumber
            """;
        command.Parameters.AddWithValue("$brandId", brandId);
        command.Parameters.AddWithValue("$songNumber", songNumber);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new ExistingSong(
            Id: reader.GetInt64(0),
            Title: reader.GetString(1),
            Language: reader.GetString(2),
            VolumeId: reader.IsDBNull(3) ? null : reader.GetInt64(3),
            PrimaryArtistId: reader.IsDBNull(4) ? null : reader.GetInt64(4));
    }

    private static long InsertSong(
        SqliteConnection connection,
        SqliteTransaction transaction,
        SongRecord song,
        long brandId,
        long? volumeId,
        long? artistId,
        string sourceFileName,
        DateTimeOffset importedAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Songs (BrandId, VolumeId, SongNumber, Title, NormalizedTitle, Language, PrimaryArtistId, SourceFileName, LastImportedAt)
            VALUES ($brandId, $volumeId, $songNumber, $title, $normalizedTitle, $language, $artistId, $sourceFileName, $lastImportedAt);
            SELECT last_insert_rowid();
            """;
        AddSongParameters(command, song, brandId, volumeId, artistId, sourceFileName, importedAt);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static void UpdateSong(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long songId,
        SongRecord song,
        long? volumeId,
        long? artistId,
        string sourceFileName,
        DateTimeOffset importedAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Songs
            SET VolumeId = COALESCE($volumeId, VolumeId),
                Title = $title,
                NormalizedTitle = $normalizedTitle,
                Language = $language,
                PrimaryArtistId = COALESCE($artistId, PrimaryArtistId),
                SourceFileName = $sourceFileName,
                LastImportedAt = $lastImportedAt
            WHERE Id = $songId
            """;
        command.Parameters.AddWithValue("$songId", songId);
        AddSongParameters(command, song, brandId: 0, volumeId, artistId, sourceFileName, importedAt);
        command.ExecuteNonQuery();
    }

    private static void AddSongParameters(
        SqliteCommand command,
        SongRecord song,
        long brandId,
        long? volumeId,
        long? artistId,
        string sourceFileName,
        DateTimeOffset importedAt)
    {
        if (brandId != 0)
        {
            command.Parameters.AddWithValue("$brandId", brandId);
        }

        command.Parameters.AddWithValue("$volumeId", volumeId is null ? DBNull.Value : volumeId);
        command.Parameters.AddWithValue("$songNumber", song.SongNumber);
        command.Parameters.AddWithValue("$title", song.Title);
        command.Parameters.AddWithValue("$normalizedTitle", Normalize(song.Title));
        command.Parameters.AddWithValue("$language", string.IsNullOrWhiteSpace(song.Language) ? "Unknown" : song.Language);
        command.Parameters.AddWithValue("$artistId", artistId is null ? DBNull.Value : artistId);
        command.Parameters.AddWithValue("$sourceFileName", sourceFileName);
        command.Parameters.AddWithValue("$lastImportedAt", importedAt.ToString("O"));
    }

    private static void UpsertSongArtist(SqliteConnection connection, SqliteTransaction transaction, long songId, long artistId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO SongArtists (SongId, ArtistId, SortOrder)
            VALUES ($songId, $artistId, 0)
            """;
        command.Parameters.AddWithValue("$songId", songId);
        command.Parameters.AddWithValue("$artistId", artistId);
        command.ExecuteNonQuery();
    }

    private static bool IsDuplicate(ExistingSong existing, SongRecord incoming, long? volumeId, long? artistId)
    {
        return string.Equals(existing.Title, incoming.Title, StringComparison.Ordinal) &&
            string.Equals(existing.Language, incoming.Language, StringComparison.Ordinal) &&
            existing.VolumeId == volumeId &&
            existing.PrimaryArtistId == artistId;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private sealed record ExistingSong(long Id, string Title, string Language, long? VolumeId, long? PrimaryArtistId);
}
