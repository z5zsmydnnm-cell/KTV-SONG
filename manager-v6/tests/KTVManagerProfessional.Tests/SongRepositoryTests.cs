using KTVManagerProfessional.Core;
using KTVManagerProfessional.Core.Data;
using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Tests;

public sealed class SongRepositoryTests
{
    [Fact]
    public void UpsertSong_returns_new_duplicate_and_updated()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ktv-{Guid.NewGuid():N}.sqlite");
        KtvDatabase.Initialize(path);
        var repository = new SongRepository(path);
        var importedAt = new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);
        var song = new SongRecord("123456", "想你的夜", "關喆", "國語", BrandCode.InYuan, "1356");

        var first = repository.UpsertSong(song, "1356.csv", importedAt);
        var duplicate = repository.UpsertSong(song, "1356.csv", importedAt);
        var updated = repository.UpsertSong(song with { Title = "想你的夜晚" }, "1356-update.csv", importedAt);

        Assert.Equal(SongWriteStatus.New, first.Status);
        Assert.Equal(SongWriteStatus.Duplicate, duplicate.Status);
        Assert.Equal(SongWriteStatus.Updated, updated.Status);
        Assert.Equal(1, repository.CountSongs());
    }

    [Fact]
    public void UpsertSong_persists_manual_song_and_updates_existing_song()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ktv-manual-{Guid.NewGuid():N}.sqlite");
        KtvDatabase.Initialize(path);
        var repository = new SongRepository(path);
        var importedAt = new DateTimeOffset(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);
        var manualSong = new SongRecord("900001", "Manual Title", "Manual Artist", "台語", BrandCode.InYuan, "3001");

        var inserted = repository.UpsertSong(manualSong, "manual", importedAt);
        var updated = repository.UpsertSong(manualSong with { Title = "Manual Title Updated" }, "manual", importedAt);

        var song = Assert.Single(repository.GetAllSongs());
        Assert.Equal(SongWriteStatus.New, inserted.Status);
        Assert.Equal(SongWriteStatus.Updated, updated.Status);
        Assert.Equal("900001", song.SongNumber);
        Assert.Equal("Manual Title Updated", song.Title);
        Assert.Equal("Manual Artist", song.Artist);
        Assert.Equal("台語", song.Language);
        Assert.Equal(BrandCode.InYuan, song.BrandCode);
        Assert.Equal("3001", song.Volume);
    }
}
