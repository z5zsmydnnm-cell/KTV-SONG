using Microsoft.Data.Sqlite;

namespace KTVManagerProfessional.Core.Data;

public static class KtvDatabase
{
    public static void Initialize(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = OpenConnection(databasePath);
        foreach (var statement in SchemaStatements)
        {
            using var command = connection.CreateCommand();
            command.CommandText = statement;
            command.ExecuteNonQuery();
        }
    }

    public static SqliteConnection OpenConnection(string databasePath)
    {
        var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        return connection;
    }

    private static readonly string[] SchemaStatements =
    [
        """
        CREATE TABLE IF NOT EXISTS Brands (
            Id INTEGER PRIMARY KEY,
            Code TEXT NOT NULL UNIQUE,
            DisplayName TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS Volumes (
            Id INTEGER PRIMARY KEY,
            BrandId INTEGER NOT NULL,
            Code TEXT NOT NULL,
            DisplayName TEXT NULL,
            CreatedAt TEXT NOT NULL,
            UNIQUE (BrandId, Code)
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS Artists (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL UNIQUE,
            NormalizedName TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS Songs (
            Id INTEGER PRIMARY KEY,
            BrandId INTEGER NOT NULL,
            VolumeId INTEGER NULL,
            SongNumber TEXT NOT NULL,
            Title TEXT NOT NULL,
            NormalizedTitle TEXT NOT NULL,
            Language TEXT NOT NULL,
            PrimaryArtistId INTEGER NULL,
            SourceFileName TEXT NOT NULL,
            LastImportedAt TEXT NOT NULL,
            UNIQUE (BrandId, SongNumber)
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS SongArtists (
            SongId INTEGER NOT NULL,
            ArtistId INTEGER NOT NULL,
            SortOrder INTEGER NOT NULL,
            PRIMARY KEY (SongId, ArtistId)
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS ImportHistory (
            Id INTEGER PRIMARY KEY,
            SourceFileName TEXT NOT NULL,
            SourcePath TEXT NOT NULL,
            SourceType TEXT NOT NULL,
            DetectedBrandCode TEXT NOT NULL,
            DetectedVolumeCode TEXT NULL,
            StartedAt TEXT NOT NULL,
            FinishedAt TEXT NULL,
            TotalRows INTEGER NOT NULL,
            ImportedRows INTEGER NOT NULL,
            UpdatedRows INTEGER NOT NULL,
            DuplicateRows INTEGER NOT NULL,
            FailedRows INTEGER NOT NULL,
            SuccessRate REAL NOT NULL,
            Status TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS ImportIssues (
            Id INTEGER PRIMARY KEY,
            ImportHistoryId INTEGER NOT NULL,
            LineNumber INTEGER NULL,
            CellReference TEXT NULL,
            RawText TEXT NOT NULL,
            Reason TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS Favorites (
            Id INTEGER PRIMARY KEY,
            SongId INTEGER NOT NULL UNIQUE,
            CreatedAt TEXT NOT NULL,
            Note TEXT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS PublishHistory (
            Id INTEGER PRIMARY KEY,
            Action TEXT NOT NULL,
            RepositoryPath TEXT NOT NULL,
            BranchName TEXT NULL,
            RemoteUrl TEXT NULL,
            CommitSha TEXT NULL,
            Message TEXT NULL,
            SelectedFilesJson TEXT NULL,
            StartedAt TEXT NOT NULL,
            FinishedAt TEXT NULL,
            ExitCode INTEGER NULL,
            StdOut TEXT NULL,
            StdErr TEXT NULL,
            Status TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS PdfPageDiagnostics (
            Id INTEGER PRIMARY KEY,
            ImportHistoryId INTEGER NOT NULL,
            PageNumber INTEGER NOT NULL,
            TextLayerCharacterCount INTEGER NOT NULL,
            SongLikeRowCount INTEGER NOT NULL,
            ParserIssueCount INTEGER NOT NULL,
            OcrRan INTEGER NOT NULL,
            OcrCharacterCount INTEGER NOT NULL,
            Confidence REAL NOT NULL,
            CreatedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS ManualCorrections (
            Id INTEGER PRIMARY KEY,
            ImportHistoryId INTEGER NOT NULL,
            PageNumber INTEGER NOT NULL,
            RawText TEXT NOT NULL,
            OcrText TEXT NULL,
            SongNumber TEXT NULL,
            Title TEXT NULL,
            ArtistName TEXT NULL,
            Language TEXT NULL,
            BrandCode TEXT NULL,
            VolumeCode TEXT NULL,
            Status TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NULL
        )
        """
    ];
}
