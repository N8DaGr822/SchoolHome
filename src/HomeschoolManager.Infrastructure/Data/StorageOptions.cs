namespace HomeschoolManager.Infrastructure.Data;

/// <summary>
/// Central configuration for where homeschool data lives on disk.
/// Bound from the "DataStorage" configuration section, with the legacy
/// flat "DataFilePath" key honored as a fallback.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "DataStorage";

    /// <summary>Legacy flat configuration key kept for backwards compatibility.</summary>
    public const string LegacyFilePathKey = "DataFilePath";

    /// <summary>
    /// Path to the homeschool data JSON file. When empty, data is stored under
    /// the user's local application data folder.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Resolves the effective data file path, falling back to
    /// %LOCALAPPDATA%/HomeschoolManager/homeschool-data.json when unconfigured.
    /// </summary>
    public string ResolveFilePath()
    {
        if (!string.IsNullOrWhiteSpace(FilePath))
        {
            return FilePath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var storageRoot = string.IsNullOrWhiteSpace(localAppData)
            ? AppContext.BaseDirectory
            : Path.Combine(localAppData, "HomeschoolManager");

        return Path.Combine(storageRoot, "homeschool-data.json");
    }
}
