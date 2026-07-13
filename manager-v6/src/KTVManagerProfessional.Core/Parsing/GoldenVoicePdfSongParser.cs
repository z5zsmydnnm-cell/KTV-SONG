using System.Text.RegularExpressions;
using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Core.Parsing;

public sealed partial class GoldenVoicePdfSongParser : ISongParser
{
    public ParseResult Parse(string text, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(text);

        var volume = DetectVolume(text, sourceName);
        var currentLanguage = string.Empty;
        var songs = new List<SongRecord>();
        var issues = new List<ParseIssue>();
        var lines = text
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n')
            .Select((line, index) => (Number: index + 1, Text: Normalize(line)))
            .Where(line => line.Text.Length > 0)
            .ToList();

        foreach (var line in lines)
        {
            if (TryDetectLanguage(line.Text, out var language))
            {
                currentLanguage = language;
                continue;
            }

            if (line.Text.Contains(BrandCode.GoldenVoice, StringComparison.Ordinal))
            {
                continue;
            }

            var match = SongLineRegex().Match(line.Text);
            if (!match.Success)
            {
                continue;
            }

            var songNumber = match.Groups["number"].Value;
            var title = match.Groups["title"].Value.Trim();
            var artist = match.Groups["artist"].Value.Trim();

            if (title.Length == 0 || artist.Length == 0)
            {
                issues.Add(new ParseIssue(line.Number, line.Text, "Song line is missing title or artist."));
                continue;
            }

            songs.Add(new SongRecord(songNumber, title, artist, currentLanguage, BrandCode.GoldenVoice, volume));
        }

        var orderedSongs = songs.OrderBy(song => song.SongNumber, StringComparer.Ordinal).ToList();
        if (orderedSongs.Count == 0 && issues.Count == 0)
        {
            issues.Add(new ParseIssue(0, sourceName, "No song rows matched the GoldenVoice PDF parser. The PDF may be scanned/image-only, have no text layer, or use an unsupported layout."));
        }

        return new ParseResult(orderedSongs, issues);
    }

    private static string Normalize(string line)
    {
        return line
            .Replace('\u00a0', ' ')
            .Replace('\u3000', ' ')
            .Trim();
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

    [GeneratedRegex(@"(?<!\d)(?<volume>\d{2,4})(?!\d)", RegexOptions.Compiled)]
    private static partial Regex VolumeRegex();

    [GeneratedRegex(@"^(?<number>\d{5,6})\s+(?<title>.+?)\s{2,}(?<artist>.+)$", RegexOptions.Compiled)]
    private static partial Regex SongLineRegex();
}
