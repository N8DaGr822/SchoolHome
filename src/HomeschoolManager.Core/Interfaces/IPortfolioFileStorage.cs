namespace HomeschoolManager.Core.Interfaces;

public interface IPortfolioFileStorage
{
    string StorageRoot { get; }
    Task<StoredPortfolioFile> SaveAsync(Stream stream, string fileName, string contentType);
    Task DeleteAsync(string storedFilePath);
}

public sealed record StoredPortfolioFile(
    string OriginalFileName,
    string StoredFileName,
    string StoredFilePath,
    string ContentType,
    long SizeBytes);
