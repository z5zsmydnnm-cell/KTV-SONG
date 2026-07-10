namespace KTVManagerProfessional.Core.Data;

public sealed class PublishHistoryRepository
{
    private readonly string _databasePath;

    public PublishHistoryRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public void Add(PublishHistoryRecord record)
    {
        using var connection = KtvDatabase.OpenConnection(_databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO PublishHistory (
                Action, RepositoryPath, BranchName, RemoteUrl, CommitSha, Message, SelectedFilesJson,
                StartedAt, FinishedAt, ExitCode, StdOut, StdErr, Status)
            VALUES (
                $action, $repositoryPath, $branchName, $remoteUrl, $commitSha, $message, $selectedFilesJson,
                $startedAt, $finishedAt, $exitCode, $stdOut, $stdErr, $status)
            """;
        command.Parameters.AddWithValue("$action", record.Action);
        command.Parameters.AddWithValue("$repositoryPath", record.RepositoryPath);
        command.Parameters.AddWithValue("$branchName", (object?)record.BranchName ?? DBNull.Value);
        command.Parameters.AddWithValue("$remoteUrl", (object?)record.RemoteUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("$commitSha", (object?)record.CommitSha ?? DBNull.Value);
        command.Parameters.AddWithValue("$message", (object?)record.Message ?? DBNull.Value);
        command.Parameters.AddWithValue("$selectedFilesJson", (object?)record.SelectedFilesJson ?? DBNull.Value);
        command.Parameters.AddWithValue("$startedAt", record.StartedAt.ToString("O"));
        command.Parameters.AddWithValue("$finishedAt", record.FinishedAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$exitCode", (object?)record.ExitCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$stdOut", (object?)record.StdOut ?? DBNull.Value);
        command.Parameters.AddWithValue("$stdErr", (object?)record.StdErr ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", record.Status);
        command.ExecuteNonQuery();
    }

    public int Count()
    {
        using var connection = KtvDatabase.OpenConnection(_databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM PublishHistory";
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
