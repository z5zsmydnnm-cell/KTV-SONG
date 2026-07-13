using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using KTVManagerProfessional.Core;
using KTVManagerProfessional.Core.Data;
using KTVManagerProfessional.Core.Git;
using KTVManagerProfessional.Core.Importing;
using KTVManagerProfessional.Core.Ocr;
using Microsoft.Win32;

namespace KTVManagerProfessional.App;

public partial class MainWindow : Window
{
    private static readonly string DatabasePath = Path.Combine(
        GitRepositorySettings.DefaultRepositoryPath,
        "manager-v6",
        "data",
        "ktv-manager-v6.sqlite");
    private static readonly string SongsDirectoryPath = SongLibraryPaths.DefaultSongsDirectoryPath;
    private static readonly string MasterCsvPath = SongLibraryPaths.DefaultMasterCsvPath;

    private readonly ObservableCollection<SongRecord> _songs = [];
    private readonly ObservableCollection<ParseIssue> _issues = [];
    private readonly ObservableCollection<ImportFileResult> _importResults = [];
    private readonly ObservableCollection<GitStatusEntry> _gitEntries = [];
    private readonly IOcrTextExtractor _ocrTextExtractor = new UnavailableOcrTextExtractor();

    public MainWindow()
    {
        InitializeComponent();
        SongsGrid.ItemsSource = _songs;
        IssuesList.ItemsSource = _issues;
        ImportResultsGrid.ItemsSource = _importResults;
        GitStatusGrid.ItemsSource = _gitEntries;
        KtvDatabase.Initialize(DatabasePath);
        RefreshSongs();
        GitStatusText.Text = BuildGitStatusText();
        FooterStatusText.Text = BuildFooterStatusText();
        OcrStatusText.Text = _ocrTextExtractor.AvailabilityMessage;
        RunOcrButton.IsEnabled = _ocrTextExtractor.IsAvailable;
    }

    private async void ChooseFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "匯入 PDF、Excel 或 CSV 檔案",
            Filter = "支援的檔案 (*.pdf;*.xlsx;*.xls;*.csv)|*.pdf;*.xlsx;*.xls;*.csv|所有檔案 (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            await ImportFilesAsync(dialog.FileNames);
        }
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_songs.Count == 0)
        {
            MessageBox.Show(this, "目前沒有歌曲可匯出。", "匯出 master.csv", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SyncMasterCsv();
        StatusText.Text = $"已同步 {_songs.Count} 首歌曲到 {MasterCsvPath}";
        await RefreshGitAsync();
    }

    private async void ManualUpsertSong_Click(object sender, RoutedEventArgs e)
    {
        var songNumber = ManualSongNumberText.Text.Trim();
        var title = ManualTitleText.Text.Trim();
        var artist = ManualArtistText.Text.Trim();
        var language = ManualLanguageText.Text.Trim();
        var brand = ManualBrandText.Text.Trim();
        var volume = ManualVolumeText.Text.Trim();

        if (songNumber.Length == 0 || title.Length == 0)
        {
            MessageBox.Show(this, "請至少輸入歌號與歌名。", "手動新增/更新", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var song = new SongRecord(
            SongNumber: songNumber,
            Title: title,
            Artist: artist,
            Language: string.IsNullOrWhiteSpace(language) ? "Unknown" : language,
            BrandCode: string.IsNullOrWhiteSpace(brand) ? "音圓" : brand,
            Volume: volume);

        var result = new SongRepository(DatabasePath).UpsertSong(song, "manual", DateTimeOffset.Now);
        RefreshSongs();
        SyncMasterCsv();
        StatusText.Text = result.Status == SongWriteStatus.New
            ? $"已手動新增 {song.SongNumber} {song.Title}，並同步到 {MasterCsvPath}"
            : $"已手動更新 {song.SongNumber} {song.Title}，並同步到 {MasterCsvPath}";

        ManualSongNumberText.Clear();
        ManualTitleText.Clear();
        ManualArtistText.Clear();
        ManualVolumeText.Clear();
        ManualSongNumberText.Focus();
        await RefreshGitAsync();
    }

    private async void ReadSongsFolder_Click(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(SongsDirectoryPath))
        {
            MessageBox.Show(this, $"找不到 songs 資料夾：{SongsDirectoryPath}", "讀取 songs", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var files = Directory
            .EnumerateFiles(SongsDirectoryPath)
            .Where(IsSupportedImportFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            MessageBox.Show(this, $"songs 資料夾沒有 PDF、Excel 或 CSV 檔案：{SongsDirectoryPath}", "讀取 songs", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await ImportFilesAsync(files);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasSupportedFiles(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (!HasSupportedFiles(e.Data))
        {
            return;
        }

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        await ImportFilesAsync(files);
    }

    private static bool HasSupportedFiles(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        return data.GetData(DataFormats.FileDrop) is string[] files
            && files.Any(IsSupportedOrReportable);
    }

    private static bool IsSupportedOrReportable(string path)
    {
        return File.Exists(path);
    }

    private static bool IsSupportedImportFile(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".pdf" or ".xlsx" or ".xls" or ".csv" => true,
            _ => false
        };
    }

    private async Task ImportFilesAsync(IReadOnlyList<string> files)
    {
        try
        {
            StatusText.Text = $"正在匯入 {files.Count} 個檔案...";
            var summary = await new ImportEngine().ImportFilesAsync(files, DatabasePath, CancellationToken.None);

            _importResults.Clear();
            foreach (var result in summary.Results)
            {
                _importResults.Add(result);
            }

            _issues.Clear();
            foreach (var issue in summary.Results.SelectMany(result => result.Issues))
            {
                _issues.Add(issue);
            }

            RefreshSongs();
            StatusText.Text = $"匯入完成：新增 {summary.ImportedRows}、更新 {summary.UpdatedRows}、重複 {summary.DuplicateRows}、失敗 {summary.FailedRows}，成功率 {summary.SuccessRate:0.0}%。";
        }
        catch (Exception ex)
        {
            StatusText.Text = "匯入失敗。";
            MessageBox.Show(this, ex.Message, "匯入失敗", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshSongs()
    {
        _songs.Clear();
        foreach (var song in new SongRepository(DatabasePath).GetAllSongs())
        {
            _songs.Add(song);
        }
    }

    private void SyncMasterCsv()
    {
        Directory.CreateDirectory(SongsDirectoryPath);
        CsvExporter.ExportMasterCsv(MasterCsvPath, _songs);
    }

    private async void RefreshGit_Click(object sender, RoutedEventArgs e)
    {
        await RefreshGitAsync();
    }

    private async void CommitGit_Click(object sender, RoutedEventArgs e)
    {
        var selectedFiles = _gitEntries
            .Select(entry => entry.Path)
            .Where(ApplicationOwnedPathFilter.IsApplicationOwned)
            .ToList();

        var message = $"data: update KTV song database {DateTime.Now:yyyy-MM-dd HH:mm}";
        var result = await new GitRepositoryService(GitRepositorySettings.DefaultRepositoryPath, DatabasePath)
            .CommitAsync(selectedFiles, message, CancellationToken.None);
        GitOutputText.Text = result.Succeeded ? result.StdOut : $"{result.StdOut}{Environment.NewLine}{result.StdErr}";
        await RefreshGitAsync();
    }

    private async void PushGit_Click(object sender, RoutedEventArgs e)
    {
        var result = await new GitRepositoryService(GitRepositorySettings.DefaultRepositoryPath, DatabasePath)
            .PushAsync(CancellationToken.None);
        GitOutputText.Text = result.Succeeded ? result.StdOut : $"{result.StdOut}{Environment.NewLine}{result.StdErr}";
        await RefreshGitAsync();
    }

    private async Task RefreshGitAsync()
    {
        var snapshot = await new GitRepositoryService(GitRepositorySettings.DefaultRepositoryPath, DatabasePath)
            .GetStatusAsync(CancellationToken.None);
        _gitEntries.Clear();
        foreach (var entry in snapshot.Entries)
        {
            _gitEntries.Add(entry);
        }

        var ownedCount = snapshot.Entries.Count(entry => ApplicationOwnedPathFilter.IsApplicationOwned(entry.Path));
        CommitButton.IsEnabled = ownedCount > 0;
        PushButton.IsEnabled = string.IsNullOrWhiteSpace(snapshot.Error);
        GitPanelStatusText.Text = $"分支：{snapshot.BranchName}；遠端：{snapshot.RemoteUrl}；程式管理變更：{ownedCount}；{snapshot.Error}";
    }

    private static string BuildGitStatusText()
    {
        var path = GitRepositorySettings.DefaultRepositoryPath;
        var status = GitRepositorySettings.IsRepository(path) ? "已偵測到 Repository" : "找不到 Repository";
        return $"GitHub 位置：{path}（{status}）";
    }

    private static string BuildFooterStatusText()
    {
        return $"資料庫：{DatabasePath}；songs：{SongsDirectoryPath}";
    }
}
