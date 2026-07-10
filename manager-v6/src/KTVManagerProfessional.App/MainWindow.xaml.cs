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
            Title = "Import PDF, Excel, or CSV files",
            Filter = "Supported files (*.pdf;*.xlsx;*.xls;*.csv)|*.pdf;*.xlsx;*.xls;*.csv|All files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            await ImportFilesAsync(dialog.FileNames);
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_songs.Count == 0)
        {
            MessageBox.Show(this, "No songs are available to export.", "Export master.csv", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export UTF-8 master.csv",
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = "master.csv"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        CsvExporter.ExportMasterCsv(dialog.FileName, _songs);
        StatusText.Text = $"Exported {_songs.Count} songs to {dialog.FileName}";
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

    private async Task ImportFilesAsync(IReadOnlyList<string> files)
    {
        try
        {
            StatusText.Text = $"Importing {files.Count} file(s)...";
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
            StatusText.Text = $"Imported {summary.ImportedRows}, updated {summary.UpdatedRows}, duplicates {summary.DuplicateRows}, failed {summary.FailedRows}. Success rate {summary.SuccessRate:0.0}%.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Import failed.";
            MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
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
        GitPanelStatusText.Text = $"Branch: {snapshot.BranchName}; Remote: {snapshot.RemoteUrl}; Application-owned changes: {ownedCount}; {snapshot.Error}";
    }

    private static string BuildGitStatusText()
    {
        var path = GitRepositorySettings.DefaultRepositoryPath;
        var status = GitRepositorySettings.IsRepository(path) ? "repo detected" : "repo not found";
        return $"GitHub path: {path} ({status})";
    }

    private static string BuildFooterStatusText()
    {
        return $"Database: {DatabasePath}";
    }
}
