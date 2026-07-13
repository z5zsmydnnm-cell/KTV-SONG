namespace KTVManagerProfessional.Core.Ocr;

public sealed record OcrWord(
    string Text,
    double X,
    double Y,
    double Width,
    double Height)
{
    public double CenterX => X + Width / 2.0;

    public double CenterY => Y + Height / 2.0;
}
