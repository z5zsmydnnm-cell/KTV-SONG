using System.Text;
using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Core;

public static class CsvExporter
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public static void ExportMasterCsv(string path, IEnumerable<SongRecord> songs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(songs);

        WriteRows(path, BuildMasterRows(songs));
    }

    public static void ExportBrandCsvs(string directoryPath, IEnumerable<SongRecord> songs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentNullException.ThrowIfNull(songs);

        Directory.CreateDirectory(directoryPath);
        var rows = BuildMasterRows(songs);
        WriteRows(
            Path.Combine(directoryPath, "音圓.csv"),
            rows.Where(row => !string.IsNullOrWhiteSpace(row.InYuanCode)).OrderBy(row => row.InYuanCode, StringComparer.Ordinal));
        WriteRows(
            Path.Combine(directoryPath, "金嗓.csv"),
            rows.Where(row => !string.IsNullOrWhiteSpace(row.GoldenVoiceCode)).OrderBy(row => row.GoldenVoiceCode, StringComparer.Ordinal));
        WriteRows(
            Path.Combine(directoryPath, "弘音.csv"),
            rows.Where(row => !string.IsNullOrWhiteSpace(row.HongYinCode)).OrderBy(row => row.HongYinCode, StringComparer.Ordinal));
    }

    private static List<MasterSongRow> BuildMasterRows(IEnumerable<SongRecord> songs)
    {
        return songs
            .GroupBy(song => new MasterSongKey(song.Title, song.Artist, song.Language), MasterSongKeyComparer.Instance)
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(song => song.Title, StringComparer.Ordinal)
                    .ThenBy(song => song.Artist, StringComparer.Ordinal)
                    .ThenBy(song => song.Language, StringComparer.Ordinal)
                    .ThenBy(song => song.BrandCode, StringComparer.Ordinal)
                    .ThenBy(song => song.SongNumber, StringComparer.Ordinal)
                    .ToList();
                var first = ordered[0];
                return new MasterSongRow(
                    Title: first.Title,
                    Artist: first.Artist,
                    Language: first.Language,
                    InYuanCode: FirstCodeForBrand(ordered, BrandCode.InYuan),
                    GoldenVoiceCode: FirstCodeForBrand(ordered, BrandCode.GoldenVoice),
                    HongYinCode: FirstCodeForBrand(ordered, "弘音"),
                    Volume: JoinDistinct(ordered.Select(song => song.Volume)),
                    Note: string.Empty);
            })
            .OrderBy(row => row.Title, StringComparer.Ordinal)
            .ThenBy(row => row.Artist, StringComparer.Ordinal)
            .ThenBy(row => row.Language, StringComparer.Ordinal)
            .ToList();
    }

    private static string FirstCodeForBrand(IReadOnlyList<SongRecord> songs, string brandCode)
    {
        return songs
            .Where(song => string.Equals(song.BrandCode, brandCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(song => song.SongNumber, StringComparer.Ordinal)
            .Select(song => song.SongNumber)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string JoinDistinct(IEnumerable<string> values)
    {
        return string.Join(
            "; ",
            values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal));
    }

    private static void WriteRows(string path, IEnumerable<MasterSongRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("歌名,歌手,語言,音圓代號,金嗓代號,弘音代號,集數,備註");

        foreach (var row in rows)
        {
            builder
                .Append(Escape(row.Title)).Append(',')
                .Append(Escape(row.Artist)).Append(',')
                .Append(Escape(row.Language)).Append(',')
                .Append(Escape(row.InYuanCode)).Append(',')
                .Append(Escape(row.GoldenVoiceCode)).Append(',')
                .Append(Escape(row.HongYinCode)).Append(',')
                .Append(Escape(row.Volume)).Append(',')
                .Append(Escape(row.Note)).AppendLine();
        }

        File.WriteAllText(path, builder.ToString(), Utf8WithBom);
    }

    private static string Escape(string value)
    {
        if (!value.Contains('"') && !value.Contains(',') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private sealed record MasterSongRow(
        string Title,
        string Artist,
        string Language,
        string InYuanCode,
        string GoldenVoiceCode,
        string HongYinCode,
        string Volume,
        string Note);

    private sealed record MasterSongKey(string Title, string Artist, string Language);

    private sealed class MasterSongKeyComparer : IEqualityComparer<MasterSongKey>
    {
        public static readonly MasterSongKeyComparer Instance = new();

        public bool Equals(MasterSongKey? x, MasterSongKey? y)
        {
            return x is not null &&
                y is not null &&
                string.Equals(Normalize(x.Title), Normalize(y.Title), StringComparison.Ordinal) &&
                string.Equals(Normalize(x.Artist), Normalize(y.Artist), StringComparison.Ordinal) &&
                string.Equals(Normalize(x.Language), Normalize(y.Language), StringComparison.Ordinal);
        }

        public int GetHashCode(MasterSongKey obj)
        {
            return HashCode.Combine(Normalize(obj.Title), Normalize(obj.Artist), Normalize(obj.Language));
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToUpperInvariant();
        }
    }
}
