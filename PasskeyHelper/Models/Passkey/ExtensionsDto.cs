using System.Text.Json.Serialization;

namespace PasskeyHelper;

public class ExtensionsDto
{
    [JsonPropertyName("credProps")]
    public CredPropsDto CredProps { get; set; } = new();
}
