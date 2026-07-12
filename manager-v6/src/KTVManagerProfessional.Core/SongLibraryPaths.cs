namespace KTVManagerProfessional.Core;

public static class SongLibraryPaths
{
    public static string DefaultSongsDirectoryPath =>
        Path.Combine(GitRepositorySettings.DefaultRepositoryPath, "songs");

    public static string DefaultMasterCsvPath =>
        Path.Combine(DefaultSongsDirectoryPath, "master.csv");
}
