using Fido2NetLib.Objects;

namespace PasskeyHelper.Models.Passkey;

public class CreateAttestationOptionsInputModel
{
    public required string UserName { get; set; }
    public string AttestationType { get; set; } = "none";
    public AuthenticatorAttachment? AuthenticatorAttachment { get; set; }
    public ResidentKeyRequirement ResidentKey { get; set; } = ResidentKeyRequirement.Discouraged;
    public UserVerificationRequirement UserVerification { get; set; } = UserVerificationRequirement.Preferred;

}
