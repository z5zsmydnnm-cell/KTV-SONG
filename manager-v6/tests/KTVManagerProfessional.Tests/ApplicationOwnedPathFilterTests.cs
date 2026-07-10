using KTVManagerProfessional.Core.Git;

namespace KTVManagerProfessional.Tests;

public sealed class ApplicationOwnedPathFilterTests
{
    [Theory]
    [InlineData("songs/master.csv", true)]
    [InlineData("songs/音圓.csv", true)]
    [InlineData("manager-v6/data/ktv-manager-v6.sqlite", true)]
    [InlineData("manager-v6/docs/release-notes/2026-07-10-1200.md", true)]
    [InlineData("manager-v6/src/KTVManagerProfessional.Core/SongRecord.cs", false)]
    [InlineData("README.md", false)]
    public void IsApplicationOwned_returns_expected_result(string path, bool expected)
    {
        Assert.Equal(expected, ApplicationOwnedPathFilter.IsApplicationOwned(path));
    }
}
