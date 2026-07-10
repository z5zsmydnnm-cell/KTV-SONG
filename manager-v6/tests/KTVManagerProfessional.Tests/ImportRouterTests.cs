using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Tests;

public sealed class ImportRouterTests
{
    [Fact]
    public void Route_detects_inyuan_pdf_from_filename()
    {
        var route = ImportRouter.Route(@"D:\in\音圓1326.pdf");

        Assert.Equal(ImportSourceType.Pdf, route.SourceType);
        Assert.Equal(BrandCode.InYuan, route.BrandCode);
        Assert.False(route.IsUnsupported);
    }

    [Fact]
    public void Route_detects_golden_voice_excel_from_filename()
    {
        var route = ImportRouter.Route(@"D:\in\金嗓112.xlsx");

        Assert.Equal(ImportSourceType.Excel, route.SourceType);
        Assert.Equal(BrandCode.GoldenVoice, route.BrandCode);
        Assert.False(route.IsUnsupported);
    }

    [Fact]
    public void Route_detects_csv_with_unknown_brand()
    {
        var route = ImportRouter.Route(@"D:\in\songs.csv");

        Assert.Equal(ImportSourceType.Csv, route.SourceType);
        Assert.Equal(BrandCode.Unknown, route.BrandCode);
        Assert.False(route.IsUnsupported);
    }

    [Fact]
    public void Route_reports_unsupported_extension()
    {
        var route = ImportRouter.Route(@"D:\in\readme.txt");

        Assert.True(route.IsUnsupported);
        Assert.Equal(ImportSourceType.Unsupported, route.SourceType);
    }
}
