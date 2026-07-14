using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace KTVManagerProfessional.Core.Ocr;

public sealed class WindowsPdfOcrPageExtractor : IPdfOcrPageExtractor
{
    private const double RenderScale = 2.5;

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
            using var stream = new InMemoryRandomAccessStream();
            var renderOptions = CreateRenderOptions(page);
            await page.RenderToStreamAsync(stream, renderOptions);
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var result = await engine.RecognizeAsync(bitmap);
            var words = ToWords(result).ToList();
            if (japaneseEngine is not null && ShouldRunJapaneseOcr(words))
            {
                var japaneseResult = await japaneseEngine.RecognizeAsync(bitmap);
                words = MergeWords(words, ToWords(japaneseResult));
            }

            pages.Add(new OcrPage(
                PageNumber: (int)index + 1,
                Width: bitmap.PixelWidth,
                Height: bitmap.PixelHeight,
                Words: words));
        }

        return pages;
    }

    private static PdfPageRenderOptions CreateRenderOptions(PdfPage page)
    {
        return new PdfPageRenderOptions
        {
            DestinationWidth = Math.Max(1, (uint)Math.Round(page.Size.Width * RenderScale)),
            DestinationHeight = Math.Max(1, (uint)Math.Round(page.Size.Height * RenderScale))
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

    private static bool IsSameWord(OcrWord left, OcrWord right)
    {
        return string.Equals(left.Text, right.Text, StringComparison.Ordinal) &&
            Math.Abs(left.CenterX - right.CenterX) <= 12 &&
            Math.Abs(left.CenterY - right.CenterY) <= 12;
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
}
