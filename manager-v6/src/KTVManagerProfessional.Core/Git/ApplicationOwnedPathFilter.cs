namespace KTVManagerProfessional.Core.Git;

public static class ApplicationOwnedPathFilter
{
    public static bool IsApplicationOwned(string path)
    {
        var normalized = path.Replace('\\', '/');
        return string.Equals(normalized, "songs/master.csv", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("songs/", StringComparison.OrdinalIgnoreCase) && normalized.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("manager-v6/data/", StringComparison.OrdinalIgnoreCase) && normalized.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("manager-v6/releases/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("manager-v6/docs/release-notes/", StringComparison.OrdinalIgnoreCase);
    }
}
