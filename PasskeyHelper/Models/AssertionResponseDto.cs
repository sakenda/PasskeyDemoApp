using System.Text.Json.Serialization;

namespace PasskeyHelper.Pages;

public partial class PasskeyLogin
{
    public class AssertionResponseDto
    {
        [JsonPropertyName("authenticatorData")]
        public string AuthenticatorData { get; set; } = "";

        [JsonPropertyName("clientDataJSON")]
        public string ClientDataJSON { get; set; } = "";

        [JsonPropertyName("signature")]
        public string Signature { get; set; } = "";
    }

}
