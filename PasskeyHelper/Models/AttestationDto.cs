using System.Text.Json.Serialization;

namespace PasskeyHelper;

public partial class PasskeyRegister
{
    public class AttestationDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("rawId")]
        public string RawId { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("extensions")]
        public ExtensionsDto Extensions { get; set; } = new();

        [JsonPropertyName("response")]
        public AttestationResponseDto Response { get; set; } = new();
    }
}
