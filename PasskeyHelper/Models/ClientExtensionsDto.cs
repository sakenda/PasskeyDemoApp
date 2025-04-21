using System.Text.Json.Serialization;

namespace PasskeyHelper.Pages;

public partial class PasskeyLogin
{
    public class ClientExtensionsDto
    {
        [JsonPropertyName("credProps")]
        public CredentialPropertiesOutputDto CredProps { get; set; } = new();
    }

}
