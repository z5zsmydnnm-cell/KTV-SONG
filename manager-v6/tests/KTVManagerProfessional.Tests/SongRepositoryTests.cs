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

    [Fact]
    public void DeleteDuplicateSongs_removes_duplicate_title_artist_language_brand_and_keeps_lowest_song_number()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ktv-duplicates-{Guid.NewGuid():N}.sqlite");
        KtvDatabase.Initialize(path);
        var repository = new SongRepository(path);
        var importedAt = new DateTimeOffset(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

        repository.UpsertSong(new SongRecord("602892", "算了吧", "", "華語", BrandCode.InYuan, "3010"), "3010.csv", importedAt);
        repository.UpsertSong(new SongRecord("200771", "算了吧", "", "華語", BrandCode.InYuan, "1333"), "1333.csv", importedAt);
        repository.UpsertSong(new SongRecord("700001", "算了吧", "不同歌手", "華語", BrandCode.InYuan, "3011"), "3011.csv", importedAt);

        var deleted = repository.DeleteDuplicateSongs();

        var songs = repository.GetAllSongs();
        Assert.Equal(1, deleted);
        Assert.Equal(2, songs.Count);
        Assert.Contains(songs, song => song.SongNumber == "200771" && song.Title == "算了吧" && song.Artist == "");
        Assert.Contains(songs, song => song.SongNumber == "700001" && song.Title == "算了吧" && song.Artist == "不同歌手");
        Assert.DoesNotContain(songs, song => song.SongNumber == "602892");
    }
}
