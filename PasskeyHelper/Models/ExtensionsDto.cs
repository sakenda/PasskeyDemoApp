using System.Text.Json.Serialization;

namespace PasskeyHelper;

public partial class PasskeyRegister
{
    public class ExtensionsDto
    {
        [JsonPropertyName("credProps")]
        public CredPropsDto CredProps { get; set; } = new();
    }
}
