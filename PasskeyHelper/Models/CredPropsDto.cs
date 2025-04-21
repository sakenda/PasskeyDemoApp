using System.Text.Json.Serialization;

namespace PasskeyHelper;

public partial class PasskeyRegister
{
    public class CredPropsDto
    {
        [JsonPropertyName("rk")]
        public bool Rk { get; set; }
    }
}
