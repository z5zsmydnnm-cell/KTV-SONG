namespace KTVManagerProfessional.Core;

public static class GitRepositorySettings
{
    public const string DefaultRepositoryPath = @"D:\GitHub\KTV-SONG";

    public static bool IsRepository(string path)
    {
        return Directory.Exists(Path.Combine(path, ".git"));
    }
}
