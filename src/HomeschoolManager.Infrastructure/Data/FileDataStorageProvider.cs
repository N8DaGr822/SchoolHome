using Microsoft.Extensions.Configuration;

namespace HomeschoolManager.Infrastructure.Data;

public sealed class FileDataStorageProvider : IDataStorageProvider
{
    private readonly string _filePath;

    public FileDataStorageProvider(IConfiguration configuration)
    {
        _filePath = Path.GetFullPath(ResolveDataFilePath(configuration));
    }

    public FileDataStorageProvider(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
    }

    public string FilePath => _filePath;
    public string BackupFilePath => $"{_filePath}.bak";

    public string Description => FilePath;
    public string BackupDescription => BackupFilePath;

    public async Task<string?> ReadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(_filePath);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task WriteAsync(string content)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(content);
                await writer.FlushAsync();
            }

            if (File.Exists(_filePath))
            {
                if (File.Exists(BackupFilePath))
                {
                    File.Delete(BackupFilePath);
                }

                File.Replace(tempPath, _filePath, BackupFilePath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, _filePath);
            }
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private static string ResolveDataFilePath(IConfiguration configuration)
    {
        var configuredPath = configuration["DataStorage:FilePath"] ?? configuration["DataFilePath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var storageRoot = string.IsNullOrWhiteSpace(localAppData)
            ? AppContext.BaseDirectory
            : Path.Combine(localAppData, "HomeschoolManager");

        return Path.Combine(storageRoot, "homeschool-data.json");
    }
}
