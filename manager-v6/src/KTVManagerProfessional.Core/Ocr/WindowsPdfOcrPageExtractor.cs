using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace KTVManagerProfessional.Core.Ocr;

public sealed class WindowsPdfOcrPageExtractor : IPdfOcrPageExtractor
{
    private const double RenderScale = 2.5;
    private const double RegionRenderScale = 3.6;
    private static readonly double[] FallbackRenderScales = [2.2, 3.0];
    private static readonly OcrRegion[] SupplementalRegions =
    [
        new(0.17, 0.055, 0.27, 0.925),
        new(0.61, 0.055, 0.26, 0.925)
    ];

    public bool IsAvailable => OperatingSystem.IsWindows() && CreatePrimaryEngine() is not null;

    public string AvailabilityMessage => IsAvailable
        ? "Windows OCR is available."
        : "Windows OCR is not available. Install Traditional Chinese OCR language support in Windows settings.";

    public async Task<IReadOnlyList<OcrPage>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
        cancellationToken.ThrowIfCancellationRequested();

        var engine = CreatePrimaryEngine() ?? throw new InvalidOperationException(AvailabilityMessage);
        var japaneseEngine = CreateJapaneseEngine();
        var file = await StorageFile.GetFileFromPathAsync(pdfPath);
        var document = await PdfDocument.LoadFromFileAsync(file);
        var pages = new List<OcrPage>();

        for (uint index = 0; index < document.PageCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var page = document.GetPage(index);
            var primary = await RecognizePageAsync(page, engine, RenderScale);
            var words = primary.Words.ToList();
            foreach (var fallbackScale in FallbackRenderScales)
            {
                var fallback = await RecognizePageAsync(page, engine, fallbackScale);
                words = MergeWords(words, ScaleWords(
                    fallback.Words,
                    primary.Width / (double)fallback.Width,
                    primary.Height / (double)fallback.Height));
            }

            var regionWords = await RecognizePageRegionsAsync(page, engine, RegionRenderScale, primary.Width, primary.Height);
            words = MergeWords(words, regionWords);

            if (japaneseEngine is not null && ShouldRunJapaneseOcr(words))
            {
                var japanese = await RecognizePageAsync(page, japaneseEngine, RenderScale);
                words = MergeWords(words, japanese.Words);
            }

            pages.Add(new OcrPage(
                PageNumber: (int)index + 1,
                Width: primary.Width,
                Height: primary.Height,
                Words: words));
        }

        return pages;
    }

    private static async Task<OcrRenderResult> RecognizePageAsync(PdfPage page, OcrEngine engine, double scale)
    {
        using var stream = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(stream, CreateRenderOptions(page, scale));
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var result = await engine.RecognizeAsync(bitmap);
        return new OcrRenderResult(bitmap.PixelWidth, bitmap.PixelHeight, ToWords(result).ToList());
    }

    private static async Task<IReadOnlyList<OcrWord>> RecognizePageRegionsAsync(
        PdfPage page,
        OcrEngine engine,
        double scale,
        int targetWidth,
        int targetHeight)
    {
        using var stream = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(stream, CreateRenderOptions(page, scale));
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var words = new List<OcrWord>();
        foreach (var region in SupplementalRegions)
        {
            var bounds = new BitmapBounds
            {
                X = (uint)Math.Round(decoder.PixelWidth * region.X),
                Y = (uint)Math.Round(decoder.PixelHeight * region.Y),
                Width = (uint)Math.Round(decoder.PixelWidth * region.Width),
                Height = (uint)Math.Round(decoder.PixelHeight * region.Height)
            };

            var transform = new BitmapTransform { Bounds = bounds };
            var bitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);
            var result = await engine.RecognizeAsync(bitmap);
            words.AddRange(ToWords(result).Select(word => new OcrWord(
                word.Text,
                (bounds.X + word.X) * targetWidth / (double)decoder.PixelWidth,
                (bounds.Y + word.Y) * targetHeight / (double)decoder.PixelHeight,
                word.Width * targetWidth / (double)decoder.PixelWidth,
                word.Height * targetHeight / (double)decoder.PixelHeight)));
        }

        return words;
    }

    private static PdfPageRenderOptions CreateRenderOptions(PdfPage page, double scale)
    {
        return new PdfPageRenderOptions
        {
            DestinationWidth = Math.Max(1, (uint)Math.Round(page.Size.Width * scale)),
            DestinationHeight = Math.Max(1, (uint)Math.Round(page.Size.Height * scale))
        };
    }

    private static IEnumerable<OcrWord> ToWords(OcrResult result)
    {
        return result.Lines
            .SelectMany(line => line.Words)
            .Select(word => new OcrWord(
                word.Text,
                word.BoundingRect.X,
                word.BoundingRect.Y,
                word.BoundingRect.Width,
                word.BoundingRect.Height));
    }

    private static List<OcrWord> MergeWords(IEnumerable<OcrWord> first, IEnumerable<OcrWord> second)
    {
        var words = first.ToList();
        foreach (var word in second)
        {
            if (words.Any(existing => IsSameWord(existing, word)))
            {
                continue;
            }

            words.Add(word);
        }

        return words;
    }

    private static IEnumerable<OcrWord> ScaleWords(IEnumerable<OcrWord> words, double scaleX, double scaleY)
    {
        return words.Select(word => new OcrWord(
            word.Text,
            word.X * scaleX,
            word.Y * scaleY,
            word.Width * scaleX,
            word.Height * scaleY));
    }

    private static bool IsSameWord(OcrWord left, OcrWord right)
    {
        return string.Equals(left.Text, right.Text, StringComparison.Ordinal) &&
            Math.Abs(left.CenterX - right.CenterX) <= 24 &&
            Math.Abs(left.CenterY - right.CenterY) <= 24;
    }

    private static bool ShouldRunJapaneseOcr(IReadOnlyList<OcrWord> words)
    {
        var text = string.Concat(words.Select(word => word.Text));
        return text.Contains("\u65e5\u8a9e", StringComparison.Ordinal) ||
            (text.Contains('\u65e5', StringComparison.Ordinal) &&
             text.Contains('\u8a9e', StringComparison.Ordinal) &&
             text.Contains("\u6b4c\u66f2", StringComparison.Ordinal));
    }

    private static OcrEngine? CreatePrimaryEngine()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var language = OcrEngine.AvailableRecognizerLanguages
            .FirstOrDefault(item => item.LanguageTag.Equals("zh-Hant-TW", StringComparison.OrdinalIgnoreCase))
            ?? OcrEngine.AvailableRecognizerLanguages
                .FirstOrDefault(item => item.LanguageTag.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
            ?? OcrEngine.AvailableRecognizerLanguages
                .FirstOrDefault(item => item.LanguageTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase));

        return language is null
            ? OcrEngine.TryCreateFromUserProfileLanguages()
            : OcrEngine.TryCreateFromLanguage(language);
    }

    private static OcrEngine? CreateJapaneseEngine()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var language = OcrEngine.AvailableRecognizerLanguages
            .FirstOrDefault(item => item.LanguageTag.StartsWith("ja", StringComparison.OrdinalIgnoreCase));

        return language is null ? null : OcrEngine.TryCreateFromLanguage(language);
    }

    private sealed record OcrRenderResult(int Width, int Height, IReadOnlyList<OcrWord> Words);

    private sealed record OcrRegion(double X, double Y, double Width, double Height);
}
