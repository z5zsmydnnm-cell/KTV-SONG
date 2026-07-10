using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using KTVManagerProfessional.Core;
using Microsoft.Win32;

namespace KTVManagerProfessional.App;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<SongRecord> _songs = [];
    private readonly ObservableCollection<ParseIssue> _issues = [];

    public MainWindow()
    {
        InitializeComponent();
        SongsGrid.ItemsSource = _songs;
        IssuesList.ItemsSource = _issues;
    }

    private void ChoosePdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "選擇音圓 PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadPdf(dialog.FileName);
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_songs.Count == 0)
        {
            MessageBox.Show(this, "目前沒有可匯出的歌曲。", "匯出 master.csv", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "匯出 UTF-8 master.csv",
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = "master.csv"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        CsvExporter.ExportMasterCsv(dialog.FileName, _songs);
        StatusText.Text = $"已匯出 {_songs.Count} 首歌到 {dialog.FileName}";
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasPdf(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!HasPdf(e.Data))
        {
            return;
        }

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        var pdfPath = files.First(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
        LoadPdf(pdfPath);
    }

    private static bool HasPdf(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        return data.GetData(DataFormats.FileDrop) is string[] files
            && files.Any(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
    }

    private void LoadPdf(string pdfPath)
    {
        try
        {
            StatusText.Text = $"讀取 PDF: {pdfPath}";
            var text = PdfTextExtractor.ExtractText(pdfPath);
            var result = InYuanSongParser.ParseText(text, Path.GetFileName(pdfPath));

            _songs.Clear();
            foreach (var song in result.Songs)
            {
                _songs.Add(song);
            }

            _issues.Clear();
            foreach (var issue in result.Issues)
            {
                _issues.Add(issue);
            }

            StatusText.Text = $"已解析 {Path.GetFileName(pdfPath)}，歌曲 {result.Songs.Count} 首，失敗行 {result.Issues.Count} 筆。";
        }
        catch (Exception ex)
        {
            StatusText.Text = "PDF 匯入失敗。";
            MessageBox.Show(this, ex.Message, "PDF 匯入失敗", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
