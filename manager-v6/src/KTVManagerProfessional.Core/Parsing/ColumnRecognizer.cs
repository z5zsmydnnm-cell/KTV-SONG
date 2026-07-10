namespace KTVManagerProfessional.Core.Parsing;

public static class ColumnRecognizer
{
    private static readonly string[] SongNumberAliases = ["歌號", "歌曲編號", "編號", "SongNumber", "Song No"];
    private static readonly string[] TitleAliases = ["歌名", "歌曲名稱", "曲名", "Title", "Song"];
    private static readonly string[] ArtistAliases = ["歌手", "演唱者", "Artist", "Singer"];
    private static readonly string[] LanguageAliases = ["語言", "Language"];
    private static readonly string[] VolumeAliases = ["集數", "期別", "Volume"];
    private static readonly string[] BrandAliases = ["品牌", "廠牌", "Brand"];

    public static ColumnMap Recognize(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> sampleRows)
    {
        ArgumentNullException.ThrowIfNull(headers);

        return new ColumnMap(
            SongNumberIndex: FindHeader(headers, SongNumberAliases) ?? InferSongNumber(sampleRows),
            TitleIndex: FindHeader(headers, TitleAliases),
            ArtistIndex: FindHeader(headers, ArtistAliases),
            LanguageIndex: FindHeader(headers, LanguageAliases),
            VolumeIndex: FindHeader(headers, VolumeAliases),
            BrandIndex: FindHeader(headers, BrandAliases));
    }

    private static int? FindHeader(IReadOnlyList<string> headers, IReadOnlyList<string> aliases)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            var header = Normalize(headers[index]);
            if (aliases.Any(alias => string.Equals(header, Normalize(alias), StringComparison.OrdinalIgnoreCase)))
            {
                return index;
            }
        }

        return null;
    }

    private static int? InferSongNumber(IReadOnlyList<IReadOnlyList<string>> sampleRows)
    {
        if (sampleRows.Count == 0)
        {
            return null;
        }

        var maxColumns = sampleRows.Max(row => row.Count);
        var bestIndex = -1;
        var bestScore = 0;

        for (var column = 0; column < maxColumns; column++)
        {
            var score = sampleRows.Count(row =>
                column < row.Count &&
                row[column].Trim().All(char.IsDigit) &&
                row[column].Trim().Length is 5 or 6);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = column;
            }
        }

        return bestIndex >= 0 ? bestIndex : null;
    }

    private static string Normalize(string value)
    {
        return value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
    }
}
