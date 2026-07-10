using KTVManagerProfessional.Core.Ocr;

namespace KTVManagerProfessional.Tests;

public sealed class OcrDecisionTests
{
    [Theory]
    [InlineData("", 0, 100, false, true)]
    [InlineData("some text", 0, 100, false, true)]
    [InlineData("some text", 3, 59.9, false, true)]
    [InlineData("some text", 3, 90, true, true)]
    [InlineData("some text", 3, 90, false, false)]
    public void ShouldRun_returns_expected_decision(string text, int songLikeRows, double parserSuccessRate, bool userRequested, bool expected)
    {
        Assert.Equal(expected, OcrDecision.ShouldRun(text, songLikeRows, parserSuccessRate, userRequested));
    }
}
