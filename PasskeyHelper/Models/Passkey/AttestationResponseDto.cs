using System.Text.Json.Serialization;

namespace PasskeyHelper;

public class AttestationResponseDto
{
    [JsonPropertyName("attestationObject")]
    public string AttestationObject { get; set; } = "";

    [JsonPropertyName("clientDataJSON")]
    public string ClientDataJSON { get; set; } = "";

    [JsonPropertyName("transports")]
    public string[] Transports { get; set; } = Array.Empty<string>();
}
