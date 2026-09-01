using Microsoft.JSInterop;

namespace HomeschoolManager.Infrastructure.Data;

public sealed class BrowserDataStorageProvider : IDataStorageProvider
{
    private const string StorageKey = "homeschoolData";
    private const string BackupStorageKey = "homeschoolData.bak";

    private readonly IJSRuntime _js;

    public BrowserDataStorageProvider(IJSRuntime js)
    {
        _js = js;
    }

    public string Description => $"Browser storage (localStorage): {StorageKey}";
    public string BackupDescription => $"Browser storage (localStorage): {BackupStorageKey}";

    public Task<string?> ReadAsync()
    {
        return _js.InvokeAsync<string?>("homeschoolData.getItem", StorageKey).AsTask();
    }

    public async Task WriteAsync(string content)
    {
        var current = await ReadAsync();
        if (current != null)
        {
            await _js.InvokeVoidAsync("homeschoolData.setItem", BackupStorageKey, current);
        }

        await _js.InvokeVoidAsync("homeschoolData.setItem", StorageKey, content);
    }
}
