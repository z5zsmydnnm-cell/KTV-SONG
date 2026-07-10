using System.Text.RegularExpressions;

namespace KTVManagerProfessional.Core;

public static partial class InYuanSongParser
{
    private const string BrandCode = "音圓";

    private static readonly string[] NoiseWords =
    [
        "勃露斯",
        "吉魯巴",
        "Shuffle",
        "R&B",
        "音圓唱片",
        "歌曲類型說明"
    ];

    public static ParseResult ParseText(string text, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(text);

        var volume = DetectVolume(text, sourceName);
        var songs = new List<SongRecord>();
        var issues = new List<ParseIssue>();
        var currentLanguage = string.Empty;
        var lines = ToLines(text);

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index].Text;

            if (TryDetectLanguage(line, out var language))
            {
                currentLanguage = language;
                continue;
            }

            if (ShouldIgnore(line) || IsDocumentHeading(line))
            {
                continue;
            }

            var match = SongLineRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var songNumber = match.Groups["number"].Value;
            var firstSegment = match.Groups["rest"].Value.Trim();
            if (firstSegment.Length == 0)
            {
                issues.Add(new ParseIssue(lines[index].Number, line, "Song line is missing title or artist."));
                continue;
            }

            var parts = new SongParts();
            AddSegment(parts, firstSegment);

            while (index + 1 < lines.Count)
            {
                var next = lines[index + 1].Text;
                if (SongLineRegex().IsMatch(next) || TryDetectLanguage(next, out _) || ShouldIgnore(next) || IsDocumentHeading(next))
                {
                    break;
                }

                index++;
                AddSegment(parts, next);
            }

            var title = Join(parts.TitleParts);
            var artist = Join(parts.ArtistParts);
            if (title.Length == 0 || artist.Length == 0)
            {
                issues.Add(new ParseIssue(lines[index].Number, line, "Song line is missing title or artist."));
                continue;
            }

            songs.Add(new SongRecord(songNumber, title, artist, currentLanguage, BrandCode, volume));
        }

        var dedupedSongs = songs
            .GroupBy(song => song.SongNumber, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(song => song.SongNumber, StringComparer.Ordinal)
            .ToList();

        return new ParseResult(dedupedSongs, issues);
    }

    private static IReadOnlyList<(int Number, string Text)> ToLines(string text)
    {
        return text
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n')
            .Select((line, index) => (Number: index + 1, Text: Normalize(line)))
            .Where(line => line.Text.Length > 0)
            .ToList();
    }

    private static string Normalize(string line)
    {
        return line
            .Replace('\u00a0', ' ')
            .Replace('\u3000', ' ')
            .Replace("\t", "  ", StringComparison.Ordinal)
            .Trim();
    }

    private static bool ShouldIgnore(string line)
    {
        return NoiseWords.Any(word => line.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDocumentHeading(string line)
    {
        return line.Contains("音圓", StringComparison.Ordinal) || line.Contains("第", StringComparison.Ordinal) && VolumeRegex().IsMatch(line);
    }

    private static bool TryDetectLanguage(string line, out string language)
    {
        foreach (var candidate in new[] { "台語", "國語", "華語", "客語" })
        {
            if (line.Contains(candidate, StringComparison.Ordinal))
            {
                language = candidate;
                return true;
            }
        }

        language = string.Empty;
        return false;
    }

    private static string DetectVolume(string text, string sourceName)
    {
        var sourceMatch = VolumeRegex().Match(sourceName ?? string.Empty);
        if (sourceMatch.Success)
        {
            return sourceMatch.Groups["volume"].Value;
        }

        var textMatch = VolumeRegex().Match(text);
        return textMatch.Success ? textMatch.Groups["volume"].Value : string.Empty;
    }

    private static void AddSegment(SongParts parts, string segment)
    {
        var delimiter = DoubleSpaceRegex().Match(segment);
        if (delimiter.Success)
        {
            var titlePart = segment[..delimiter.Index].Trim();
            var artistPart = segment[(delimiter.Index + delimiter.Length)..].Trim();
            if (titlePart.Length > 0)
            {
                parts.TitleParts.Add(titlePart);
            }

            if (artistPart.Length > 0)
            {
                parts.ArtistParts.Add(artistPart);
            }

            return;
        }

        if (parts.ArtistParts.Count > 0)
        {
            parts.ArtistParts.Add(segment);
        }
        else
        {
            parts.TitleParts.Add(segment);
        }
    }

    private static string Join(IEnumerable<string> parts)
    {
        return string.Join(' ', parts.Select(part => SingleSpaceRegex().Replace(part, " ").Trim()).Where(part => part.Length > 0));
    }

    private sealed class SongParts
    {
        public List<string> TitleParts { get; } = [];

        public List<string> ArtistParts { get; } = [];
    }

    [GeneratedRegex(@"(?<!\d)(?<volume>\d{4})(?!\d)", RegexOptions.Compiled)]
    private static partial Regex VolumeRegex();

    [GeneratedRegex(@"^(?:[◆◇★☆●◎■□*]+\s*)?(?<number>\d{5,6})(?:\s+(?<rest>.*))?$", RegexOptions.Compiled)]
    private static partial Regex SongLineRegex();

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex DoubleSpaceRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex SingleSpaceRegex();
}
