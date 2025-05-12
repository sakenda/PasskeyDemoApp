namespace ActiveDirectoryHelper.Models;

/// <summary>
/// Interface für den LDAP-Service
/// </summary>
public interface IAdLdapService
{
    /// <summary>
    /// Authentifiziert einen Benutzer mittels Benutzername und Passwort
    /// </summary>
    /// <param name="username">Benutzername (entweder SAM Account Name oder E-Mail)</param>
    /// <param name="password">Passwort</param>
    /// <returns>AD-Benutzerinformationen mit Claims</returns>
    Task<AdUserInfo> AuthenticateUserAsync(string username, string password);

    /// <summary>
    /// Ermittelt Benutzerinformationen anhand einer E-Mail-Adresse
    /// </summary>
    /// <param name="email">E-Mail-Adresse des Benutzers</param>
    /// <returns>AD-Benutzerinformationen mit Claims</returns>
    Task<AdUserInfo> GetUserInfoByEmailAsync(string email);

    /// <summary>
    /// Ermittelt Benutzerinformationen anhand des SAM Account-Namens
    /// </summary>
    /// <param name="samAccountName">SAM Account-Name (Windows-Anmeldename)</param>
    /// <returns>AD-Benutzerinformationen mit Claims</returns>
    Task<AdUserInfo> GetUserInfoBySamAccountNameAsync(string samAccountName);
}

