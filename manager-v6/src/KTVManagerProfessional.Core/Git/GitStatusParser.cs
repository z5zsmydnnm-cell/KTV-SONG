namespace KTVManagerProfessional.Core.Git;

public static class GitStatusParser
{
    public static IReadOnlyList<GitStatusEntry> ParsePorcelain(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        return output
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseLine)
            .ToList();
    }

    private static GitStatusEntry ParseLine(string line)
    {
        var raw = line.Length >= 2 ? line[..2] : line;
        var path = line.Length > 3 ? line[3..].Trim() : string.Empty;
        var status = raw switch
        {
            "??" => GitFileStatus.Untracked,
            _ when raw.Contains('A') => GitFileStatus.Added,
            _ when raw.Contains('D') => GitFileStatus.Deleted,
            _ when raw.Contains('R') => GitFileStatus.Renamed,
            _ when raw.Contains('M') => GitFileStatus.Modified,
            _ => GitFileStatus.Unknown
        };

        return new GitStatusEntry(path, status, raw);
    }
}
