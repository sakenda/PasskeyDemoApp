using System.Text.Json.Serialization;

namespace PasskeyHelper.Pages;

public class AssertionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("rawId")]
    public string RawId { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("response")]
    public AssertionResponseDto Response { get; set; } = new();

    [JsonPropertyName("extensions")]
    public ClientExtensionsDto Extensions { get; set; } = new();

}
