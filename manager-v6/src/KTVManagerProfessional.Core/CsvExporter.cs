using System.Text;

namespace KTVManagerProfessional.Core;

public static class CsvExporter
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public static void ExportMasterCsv(string path, IEnumerable<SongRecord> songs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(songs);

        var builder = new StringBuilder();
        builder.AppendLine("歌號,歌名,歌手,語言,音圓代號,集數");

        foreach (var song in songs)
        {
            builder
                .Append(Escape(song.SongNumber)).Append(',')
                .Append(Escape(song.Title)).Append(',')
                .Append(Escape(song.Artist)).Append(',')
                .Append(Escape(song.Language)).Append(',')
                .Append(Escape(song.BrandCode)).Append(',')
                .Append(Escape(song.Volume)).AppendLine();
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
}
