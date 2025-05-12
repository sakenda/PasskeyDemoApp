using Microsoft.AspNetCore.Identity;

namespace PasskeyHelper.Data;

public class ApplicationUser : IdentityUser
{
    public ICollection<PublicKeyCredential> PublicKeyCredentials { get; } = [];

}