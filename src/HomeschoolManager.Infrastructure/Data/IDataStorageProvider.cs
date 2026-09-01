namespace HomeschoolManager.Infrastructure.Data;

public interface IDataStorageProvider
{
    string Description { get; }
    string BackupDescription { get; }
    Task<string?> ReadAsync();
    Task WriteAsync(string content);
}
