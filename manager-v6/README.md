# KTV Manager Professional Build 001

Windows 11 / Visual Studio 2022 Community / C# / .NET 8 / WPF project.

## Open And Run

1. Open `manager-v6/src/KTVManagerProfessional.sln` in Visual Studio 2022 Community.
2. Set `KTVManagerProfessional.App` as the startup project.
3. Press F5.

Command-line verification:

```powershell
dotnet restore manager-v6/src/KTVManagerProfessional.sln
dotnet build manager-v6/src/KTVManagerProfessional.sln --configuration Release
dotnet test manager-v6/src/KTVManagerProfessional.sln --configuration Release --no-build
```

## Build 001 Features

- WPF only; no Windows Forms types.
- Drag PDF files from Windows File Explorer into the WPF window.
- Choose a PDF with a button.
- Extract PDF text with NuGet package `itext` 9.7.0.
- Parse InYuan PDF text:
  - Volume 1326 format with a leading `◆` type symbol.
  - Volume 1356 format without a leading type symbol.
- Display song number, title, artist, language, InYuan code, and volume.
- Export UTF-8 `master.csv`, one song per row.
- Show failed parse lines with line number, original text, and reason.

## Not In Build 001

- SQLite
- GitHub auto push
- Apple TV
- Scriptable
- YouTube

## Structure

```text
manager-v6/
  src/
    KTVManagerProfessional.sln
    KTVManagerProfessional.App/
    KTVManagerProfessional.Core/
  tests/
    KTVManagerProfessional.Tests/
  docs/
  samples/
  releases/
```

## License Note

PDF text extraction uses the `itext` NuGet package. Confirm iText licensing before commercial or closed-source distribution.
