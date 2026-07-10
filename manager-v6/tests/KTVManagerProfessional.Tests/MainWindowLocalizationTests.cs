namespace KTVManagerProfessional.Tests;

public sealed class MainWindowLocalizationTests
{
    [Fact]
    public void MainWindow_uses_chinese_user_facing_text()
    {
        var root = FindManagerRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "KTVManagerProfessional.App", "MainWindow.xaml"));
        var codeBehind = File.ReadAllText(Path.Combine(root, "src", "KTVManagerProfessional.App", "MainWindow.xaml.cs"));

        Assert.Contains("選擇檔案", xaml);
        Assert.Contains("匯出 master.csv", xaml);
        Assert.Contains("歌號", xaml);
        Assert.Contains("解析失敗行", xaml);
        Assert.Contains("正在匯入", codeBehind);
        Assert.Contains("匯入完成", codeBehind);

        Assert.DoesNotContain("Import files", xaml);
        Assert.DoesNotContain("Song No", xaml);
        Assert.DoesNotContain("Importing {files.Count} file(s)", codeBehind);
    }

    private static string FindManagerRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "manager-v6");
            if (Directory.Exists(Path.Combine(candidate, "src", "KTVManagerProfessional.App")))
            {
                return candidate;
            }

            if (Directory.Exists(Path.Combine(directory.FullName, "src", "KTVManagerProfessional.App")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Cannot locate manager-v6 root.");
    }
}
