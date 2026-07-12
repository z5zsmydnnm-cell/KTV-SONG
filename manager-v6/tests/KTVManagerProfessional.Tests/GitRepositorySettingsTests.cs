using KTVManagerProfessional.Core;

namespace KTVManagerProfessional.Tests;

public sealed class GitRepositorySettingsTests
{
    [Fact]
    public void DefaultRepositoryPath_points_to_ktv_song_repository()
    {
        Assert.Equal(@"D:\GitHub\KTV-SONG", GitRepositorySettings.DefaultRepositoryPath);
    }

    [Fact]
    public void SongLibraryPaths_point_to_repository_songs_folder()
    {
        Assert.Equal(@"D:\GitHub\KTV-SONG\songs", SongLibraryPaths.DefaultSongsDirectoryPath);
        Assert.Equal(@"D:\GitHub\KTV-SONG\songs\master.csv", SongLibraryPaths.DefaultMasterCsvPath);
    }

    [Fact]
    public void IsRepository_returns_true_when_git_directory_exists()
    {
        Assert.True(GitRepositorySettings.IsRepository(@"D:\GitHub\KTV-SONG"));
    }
}
