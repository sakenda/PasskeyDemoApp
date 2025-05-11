using System.Text.Json.Serialization;

namespace PasskeyHelper.Pages;

public class CredentialPropertiesOutputDto
{
    [JsonPropertyName("rk")]
    public bool Rk { get; set; } = false;
}
