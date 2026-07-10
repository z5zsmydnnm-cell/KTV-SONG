using KTVManagerProfessional.Core.Corrections;
using KTVManagerProfessional.Core.Data;
using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Tests;

public sealed class ManualCorrectionServiceTests
{
    [Fact]
    public void AcceptCorrection_inserts_song_and_ignore_does_not()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ktv-{Guid.NewGuid():N}.sqlite");
        KtvDatabase.Initialize(path);
        var service = new ManualCorrectionService(path);

        service.AcceptCorrection(new ManualCorrection(
            ImportHistoryId: 1,
            PageNumber: 1,
            RawText: "123456 想你的夜 關喆",
            OcrText: null,
            SongNumber: "123456",
            Title: "想你的夜",
            ArtistName: "關喆",
            Language: "國語",
            BrandCode: BrandCode.InYuan,
            VolumeCode: "1356"));
        service.IgnoreCorrection(new ManualCorrection(
            ImportHistoryId: 1,
            PageNumber: 1,
            RawText: "bad row",
            OcrText: null,
            SongNumber: null,
            Title: null,
            ArtistName: null,
            Language: null,
            BrandCode: null,
            VolumeCode: null));

        Assert.Equal(1, new SongRepository(path).CountSongs());
    }
}
