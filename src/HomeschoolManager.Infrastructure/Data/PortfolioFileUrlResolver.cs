using Microsoft.JSInterop;

namespace HomeschoolManager.Infrastructure.Data;

public class PortfolioFileUrlResolver
{
    private readonly IJSRuntime _js;

    public PortfolioFileUrlResolver(IJSRuntime js)
    {
        _js = js;
    }

    public Task<string?> GetUrlAsync(string storedFilePath)
    {
        if (string.IsNullOrWhiteSpace(storedFilePath))
        {
            return Task.FromResult<string?>(null);
        }

        return _js.InvokeAsync<string?>("homeschoolData.getPortfolioFileUrl", storedFilePath).AsTask();
    }
}
