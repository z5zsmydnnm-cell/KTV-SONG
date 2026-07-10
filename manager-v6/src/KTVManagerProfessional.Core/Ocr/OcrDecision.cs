namespace KTVManagerProfessional.Core.Ocr;

public static class OcrDecision
{
    public static bool ShouldRun(string extractedText, int songLikeRowCount, double parserSuccessRate, bool userRequested)
    {
        return userRequested ||
            string.IsNullOrWhiteSpace(extractedText) ||
            songLikeRowCount <= 0 ||
            parserSuccessRate < 60.0;
    }
}
