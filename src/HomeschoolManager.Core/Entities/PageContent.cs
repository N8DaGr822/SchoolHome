namespace HomeschoolManager.Core.Entities;

public sealed class PageContent
{
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public PageContent() { }

    public PageContent(string heading, string body)
    {
        Heading = heading;
        Body = body;
    }
}
