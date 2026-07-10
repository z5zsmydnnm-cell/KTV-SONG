using Microsoft.Data.Sqlite;
using KTVManagerProfessional.Core.Data;

namespace KTVManagerProfessional.Tests;

public sealed class KtvDatabaseTests
{
    [Fact]
    public void Initialize_creates_build_002_tables()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ktv-{Guid.NewGuid():N}.sqlite");

        KtvDatabase.Initialize(path);

        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        using var reader = command.ExecuteReader();
        var tables = new HashSet<string>();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("Brands", tables);
        Assert.Contains("Volumes", tables);
        Assert.Contains("Artists", tables);
        Assert.Contains("Songs", tables);
        Assert.Contains("SongArtists", tables);
        Assert.Contains("ImportHistory", tables);
        Assert.Contains("ImportIssues", tables);
        Assert.Contains("Favorites", tables);
    }
}
