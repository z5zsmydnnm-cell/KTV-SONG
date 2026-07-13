namespace KTVManagerProfessional.Core;

public static class SongsFolderImportFilter
{
    private static readonly HashSet<string> GeneratedSongsFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "master.csv",
        "音圓.csv",
        "金嗓.csv",
        "弘音.csv",
        "iphone-local-songs.csv"
    };

    public static bool IsImportSourceFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".pdf" or ".xlsx" or ".xls" or ".csv"))
        {
            return false;
        }

        return !GeneratedSongsFiles.Contains(Path.GetFileName(path));
    }
}
