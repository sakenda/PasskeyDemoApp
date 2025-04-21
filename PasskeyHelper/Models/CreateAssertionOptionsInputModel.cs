using Fido2NetLib.Objects;

namespace PasskeyHelper.Handlers;

public class CreateAssertionOptionsInputModel
{
    public required string UserName { get; set; }

    public UserVerificationRequirement UserVerification { get; set; } = UserVerificationRequirement.Preferred;
}
