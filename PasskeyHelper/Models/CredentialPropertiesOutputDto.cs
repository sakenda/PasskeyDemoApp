using System.Text.Json.Serialization;

namespace PasskeyHelper.Pages;

public partial class PasskeyLogin
{
    public class CredentialPropertiesOutputDto
    {
        [JsonPropertyName("rk")]
        public bool Rk { get; set; } = false;
    }

}
