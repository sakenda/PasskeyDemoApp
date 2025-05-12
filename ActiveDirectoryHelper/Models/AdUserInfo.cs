using System.Security.Claims;

namespace ActiveDirectoryHelper.Models;

/// <summary>
/// Modell für AD-Benutzerinformationen
/// </summary>
public class AdUserInfo
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string SamAccountName { get; set; } = "";
    public string DistinguishedName { get; set; } = "";
    public List<string> MemberOf { get; set; } = [];
    public List<Claim> Claims { get; set; } = [];
    public bool IsAuthenticated { get; set; }
}

