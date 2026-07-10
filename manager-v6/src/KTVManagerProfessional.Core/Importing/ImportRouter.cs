namespace KTVManagerProfessional.Core.Importing;

public static class ImportRouter
{
    public static ImportRoute Route(string sourcePath, string? contentHint = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var sourceType = DetectSourceType(sourcePath);
        if (sourceType == ImportSourceType.Unsupported)
        {
            return new ImportRoute(sourcePath, sourceType, BrandCode.Unknown, IsUnsupported: true, "Unsupported file extension.");
        }

        return new ImportRoute(sourcePath, sourceType, DetectBrand(sourcePath, contentHint), IsUnsupported: false, string.Empty);
    }

    private static ImportSourceType DetectSourceType(string sourcePath)
    {
        return Path.GetExtension(sourcePath).ToLowerInvariant() switch
        {
            ".pdf" => ImportSourceType.Pdf,
            ".xlsx" => ImportSourceType.Excel,
            ".xls" => ImportSourceType.Excel,
            ".csv" => ImportSourceType.Csv,
            _ => ImportSourceType.Unsupported
        };
    }

    private static string DetectBrand(string sourcePath, string? contentHint)
    {
        var haystack = $"{Path.GetFileNameWithoutExtension(sourcePath)} {contentHint}".ToLowerInvariant();
        if (haystack.Contains("音圓", StringComparison.Ordinal) || haystack.Contains("inyuan", StringComparison.OrdinalIgnoreCase))
        {
            return BrandCode.InYuan;
        }

        if (haystack.Contains("金嗓", StringComparison.Ordinal) || haystack.Contains("goldenvoice", StringComparison.OrdinalIgnoreCase))
        {
            return BrandCode.GoldenVoice;
        }

        return BrandCode.Unknown;
    }
}
