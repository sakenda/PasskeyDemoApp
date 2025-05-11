using System.Text.Json.Serialization;

namespace PasskeyHelper;

public class CredPropsDto
{
    [JsonPropertyName("rk")]
    public bool Rk { get; set; }
}
