using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Infrastructure.Data;

public class PortfolioFileStorage : IPortfolioFileStorage
{
    private readonly HomeschoolDataStore _dataStore;

    public PortfolioFileStorage(HomeschoolDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public string StorageRoot
    {
        get
        {
            var dataDirectory = Path.GetDirectoryName(_dataStore.FilePath) ?? AppContext.BaseDirectory;
            return Path.Combine(dataDirectory, "PortfolioFiles");
        }
    }

    public async Task<StoredPortfolioFile> SaveAsync(Stream stream, string fileName, string contentType)
    {
        if (stream.Length <= 0)
        {
            throw new InvalidOperationException("The selected file is empty.");
        }

        Directory.CreateDirectory(StorageRoot);
        var originalFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storedFilePath = Path.Combine(StorageRoot, storedFileName);

        await using (var output = new FileStream(storedFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(output);
        }

        return new StoredPortfolioFile(
            originalFileName,
            storedFileName,
            storedFilePath,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            new FileInfo(storedFilePath).Length);
    }

    public Task DeleteAsync(string storedFilePath)
    {
        if (!string.IsNullOrWhiteSpace(storedFilePath) && File.Exists(storedFilePath))
        {
            File.Delete(storedFilePath);
        }

        return Task.CompletedTask;
    }
}
