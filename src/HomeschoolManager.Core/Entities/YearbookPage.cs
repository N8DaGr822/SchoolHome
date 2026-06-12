namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeschoolManager.Core.Interfaces;

public class YearbookPage : IEntity
{
    private static readonly JsonSerializerOptions ContentSerializerOptions = new(JsonSerializerDefaults.Web);

    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Yearbook is required.")]
    public int YearbookId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Sort order must be non-negative.")]
    public int SortOrder { get; set; }

    public bool IsHidden { get; set; }

    [Required]
    public string ContentJson { get; set; } = "{}";

    [JsonIgnore]
    public PageContent Content
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ContentJson) || ContentJson.Trim() == "{}")
            {
                return new PageContent(Title, string.Empty);
            }

            try
            {
                return JsonSerializer.Deserialize<PageContent>(ContentJson, ContentSerializerOptions)
                       ?? new PageContent(Title, string.Empty);
            }
            catch (JsonException)
            {
                return new PageContent(Title, ContentJson);
            }
        }
        set => ContentJson = JsonSerializer.Serialize(value, ContentSerializerOptions);
    }

    public List<PageElement> Elements { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Yearbook Yearbook { get; set; } = null!;
}
