using HomeschoolManager.Core.Interfaces;
using Microsoft.JSInterop;

namespace HomeschoolManager.Infrastructure.Data;

public class BrowserPortfolioFileStorage : IPortfolioFileStorage
{
    private readonly IJSRuntime _js;

    public BrowserPortfolioFileStorage(IJSRuntime js)
    {
        _js = js;
    }

    public string StorageRoot => "Browser storage (IndexedDB)";

    public async Task<StoredPortfolioFile> SaveAsync(Stream stream, string fileName, string contentType)
    {
        if (stream.Length <= 0)
        {
            throw new InvalidOperationException("The selected file is empty.");
        }

        var originalFileName = Path.GetFileName(fileName);
        var resolvedContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        var sizeBytes = stream.Length;

        // The IndexedDB key doubles as the stored "path" for this implementation - there is
        // no filesystem path in the browser, so callers that display StoredFilePath just see the key.
        var id = Guid.NewGuid().ToString("N");

        using var streamRef = new DotNetStreamReference(stream, leaveOpen: false);
        await _js.InvokeVoidAsync("homeschoolData.savePortfolioFile", id, resolvedContentType, originalFileName, streamRef);

        return new StoredPortfolioFile(originalFileName, id, id, resolvedContentType, sizeBytes);
    }

    public Task DeleteAsync(string storedFilePath)
    {
        if (string.IsNullOrWhiteSpace(storedFilePath))
        {
            return Task.CompletedTask;
        }

        return _js.InvokeVoidAsync("homeschoolData.deletePortfolioFile", storedFilePath).AsTask();
    }
}
