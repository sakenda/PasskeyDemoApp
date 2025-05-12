using ActiveDirectoryHelper.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Claims;

namespace ActiveDirectoryHelper;

/// <summary>
/// Implementierung des LDAP-Services zur Abfrage von AD-Benutzerinformationen und Claims
/// Diese Implementierung ist plattformunabhängig und funktioniert auch auf Linux
/// </summary>
public class AdLdapService : IAdLdapService
{
    private readonly ILogger<AdLdapService> _logger;
    private readonly LdapConfiguration _config;

    public AdLdapService(IConfiguration configuration, ILogger<AdLdapService> logger)
    {
        _logger = logger;
        _config = new LdapConfiguration();
        configuration.GetSection(nameof(LdapConfiguration)).Bind(_config);
    }

    /// <summary>
    /// Konstruktor mit expliziter Konfiguration
    /// </summary>
    public AdLdapService(LdapConfiguration config, ILogger<AdLdapService> logger)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Authentifiziert einen Benutzer mittels Benutzername und Passwort
    /// </summary>
    public async Task<AdUserInfo> AuthenticateUserAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Benutzername und Passwort müssen angegeben werden.");
        }

        try
        {
            // Bestimmen, ob es sich bei dem Benutzernamen um eine E-Mail handelt
            bool isEmail = username.Contains('@');
            string samAccountName = username;

            // Falls E-Mail, dann SAM Account Name ermitteln
            if (isEmail)
            {
                var userInfo = await GetUserInfoByEmailAsync(username);
                if (userInfo == null)
                {
                    return new AdUserInfo { IsAuthenticated = false };
                }
                samAccountName = userInfo.SamAccountName;
            }

            // Authentifizierung via LDAP versuchen (plattformunabhängig)
            using var ldapConnection = CreateLdapConnection();
            
            string userDn;

            if (isEmail)
            {
                // Bei E-Mail erst den DN ermitteln
                var tempConn = CreateLdapConnection(true);
                userDn = await GetUserDnByEmailAsync(username, tempConn);
                tempConn.Dispose();
            }
            else
            {
                // Bei SAM Account Name den DN ermitteln
                var tempConn = CreateLdapConnection(true);
                userDn = await GetUserDnBySamAccountNameAsync(samAccountName, tempConn);
                tempConn.Dispose();
            }

            if (string.IsNullOrEmpty(userDn))
            {
                return new AdUserInfo { IsAuthenticated = false };
            }

            // Authentifizieren mit dem gefundenen DN
            ldapConnection.Credential = new NetworkCredential(userDn, password);

            try
            {
                ldapConnection.Bind();

                // Benutzerinformationen mit Service-Account abrufen (um alle Informationen zu erhalten)
                var serviceConn = CreateLdapConnection(true);
                AdUserInfo userInfo;

                if (isEmail)
                {
                    userInfo = await GetUserInfoByEmailAsync(username, serviceConn);
                }
                else
                {
                    userInfo = await GetUserInfoBySamAccountNameAsync(samAccountName, serviceConn);
                }

                serviceConn.Dispose();

                if (userInfo != null)
                {
                    userInfo.IsAuthenticated = true;
                }

                return userInfo ?? new AdUserInfo { IsAuthenticated = false };
            }
            catch (LdapException)
            {
                // Authentifizierung fehlgeschlagen
                return new AdUserInfo { IsAuthenticated = false };
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Benutzerauthentifizierung für {Username}", username);
            throw new InvalidOperationException($"Fehler bei der Benutzerauthentifizierung: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ermittelt Benutzerinformationen anhand einer E-Mail-Adresse
    /// </summary>
    public async Task<AdUserInfo> GetUserInfoByEmailAsync(string email)
    {
        using var connection = CreateLdapConnection(true);
        return await GetUserInfoByEmailAsync(email, connection);
    }

    /// <summary>
    /// Ermittelt Benutzerinformationen anhand einer E-Mail-Adresse mit bestehender Verbindung
    /// </summary>
    private async Task<AdUserInfo> GetUserInfoByEmailAsync(string email, LdapConnection connection)
    {
        if (string.IsNullOrEmpty(email))
        {
            throw new ArgumentException("E-Mail-Adresse muss angegeben werden.");
        }

        try
        {
            // LDAP-Filter für E-Mail-Suche
            string filter = $"(&(objectClass=user)(mail={email}))";
            return await SearchUserAsync(filter, connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der Benutzerinformationen für E-Mail {Email}", email);
            throw new InvalidOperationException($"Fehler beim Abrufen der Benutzerinformationen: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ermittelt den DN eines Benutzers anhand der E-Mail-Adresse
    /// </summary>
    private async Task<string?> GetUserDnByEmailAsync(string email, LdapConnection connection)
    {
        try
        {
            // LDAP-Filter für E-Mail-Suche
            string filter = $"(&(objectClass=user)(mail={email}))";

            // LDAP-Suchanfrage erstellen
            SearchRequest searchRequest = new(
                _config.SearchBase,
                filter,
                SearchScope.Subtree,
                [ "distinguishedName" ]
            );

            // Suche ausführen
            SearchResponse response = (SearchResponse)await Task.Run(() => connection.SendRequest(searchRequest));

            // Wenn kein Benutzer gefunden wurde
            if (response.Entries.Count == 0)
            {
                return null;
            }

            // DN des ersten gefundenen Benutzers zurückgeben
            return GetAttributeValue(response.Entries[0], "distinguishedName");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Ermitteln des DN für E-Mail {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Ermittelt Benutzerinformationen anhand des SAM Account-Namens
    /// </summary>
    public async Task<AdUserInfo> GetUserInfoBySamAccountNameAsync(string samAccountName)
    {
        using var connection = CreateLdapConnection(true);
        return await GetUserInfoBySamAccountNameAsync(samAccountName, connection);
    }

    /// <summary>
    /// Ermittelt Benutzerinformationen anhand des SAM Account-Namens mit bestehender Verbindung
    /// </summary>
    private async Task<AdUserInfo> GetUserInfoBySamAccountNameAsync(string samAccountName, LdapConnection connection)
    {
        if (string.IsNullOrEmpty(samAccountName))
        {
            throw new ArgumentException("SAM Account-Name muss angegeben werden.");
        }

        try
        {
            // LDAP-Filter für SAM Account-Suche
            string filter = $"(&(objectClass=user)(sAMAccountName={samAccountName}))";
            return await SearchUserAsync(filter, connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der Benutzerinformationen für SAM-Account {SamAccountName}", samAccountName);
            throw new InvalidOperationException($"Fehler beim Abrufen der Benutzerinformationen: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ermittelt den DN eines Benutzers anhand des SAM Account-Namens
    /// </summary>
    private async Task<string?> GetUserDnBySamAccountNameAsync(string samAccountName, LdapConnection connection)
    {
        try
        {
            // LDAP-Filter für SAM Account-Suche
            string filter = $"(&(objectClass=user)(sAMAccountName={samAccountName}))";

            // LDAP-Suchanfrage erstellen
            SearchRequest searchRequest = new(
                _config.SearchBase,
                filter,
                SearchScope.Subtree,
                [ "distinguishedName" ]
            );

            // Suche ausführen
            SearchResponse response = (SearchResponse)await Task.Run(() => connection.SendRequest(searchRequest));

            // Wenn kein Benutzer gefunden wurde
            if (response.Entries.Count == 0)
            {
                return null;
            }

            // DN des ersten gefundenen Benutzers zurückgeben
            return GetAttributeValue(response.Entries[0], "distinguishedName");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Ermitteln des DN für SAM-Account {SamAccountName}", samAccountName);
            return null;
        }
    }

    /// <summary>
    /// Erstellt eine neue LDAP-Verbindung
    /// </summary>
    private LdapConnection CreateLdapConnection(bool useServiceAccount = false)
    {
        LdapDirectoryIdentifier ldapDirectoryIdentifier = new(_config.LdapServer, _config.LdapPort);
        LdapConnection connection = new(ldapDirectoryIdentifier);

        if (_config.UseSSL)
        {
            connection.SessionOptions.SecureSocketLayer = true;
            connection.SessionOptions.VerifyServerCertificate = (conn, cert) => true; // Im Produktivbetrieb entsprechend anpassen!
        }

        // Bei Bedarf Anmeldung mit Service-Account
        if (useServiceAccount)
        {
            if (string.IsNullOrEmpty(_config.ServiceAccountUsername) || string.IsNullOrEmpty(_config.ServiceAccountPassword))
            {
                throw new InvalidOperationException("Service-Account-Zugriffsdaten fehlen oder sind unvollständig.");
            }

            connection.Credential = new NetworkCredential(_config.ServiceAccountUsername, _config.ServiceAccountPassword);
        }

        connection.Bind();
        return connection;
    }

    /// <summary>
    /// Sucht einen Benutzer im AD und ermittelt alle zugehörigen Informationen und Claims
    /// </summary>
    private async Task<AdUserInfo?> SearchUserAsync(string filter, LdapConnection connection)
    {
        return await Task.Run(() =>
        {
            try
            {
                // LDAP-Suchanfrage erstellen
                SearchRequest searchRequest = new(
                    _config.SearchBase,
                    filter,
                    SearchScope.Subtree,
                    [
                        "distinguishedName", "mail", "displayName",
                        "sAMAccountName", "memberOf", "objectSid"
                    ]
                );

                // Suche ausführen
                SearchResponse response = (SearchResponse)connection.SendRequest(searchRequest);

                // Wenn kein Benutzer gefunden wurde
                if (response.Entries.Count == 0)
                {
                    return null;
                }

                // Ersten Eintrag verwenden (Filter sollte ohnehin nur einen Benutzer zurückgeben)
                SearchResultEntry entry = response.Entries[0];

                // Benutzerinformationen extrahieren
                var userInfo = new AdUserInfo
                {
                    DisplayName = GetAttributeValue(entry, "displayName"),
                    Email = GetAttributeValue(entry, "mail"),
                    SamAccountName = GetAttributeValue(entry, "sAMAccountName"),
                    DistinguishedName = GetAttributeValue(entry, "distinguishedName")
                };

                // Gruppenmitgliedschaften und vererbte Gruppenmitgliedschaften ermitteln
                userInfo.MemberOf = GetMemberOfValues(entry);
                userInfo.Claims = GetUserClaims(userInfo, connection);

                return userInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Suchen des Benutzers mit Filter {Filter}", filter);
                throw;
            }
        });
    }

    /// <summary>
    /// Liest den Wert eines LDAP-Attributs
    /// </summary>
    private static string GetAttributeValue(SearchResultEntry entry, string attributeName)
    {
        if (entry.Attributes.Contains(attributeName) && entry.Attributes[attributeName].Count > 0)
        {
            return entry.Attributes[attributeName][0].ToString()!;
        }

        return null!;
    }

    /// <summary>
    /// Ermittelt alle Gruppen, in denen ein Benutzer direkt Mitglied ist
    /// </summary>
    private static List<string> GetMemberOfValues(SearchResultEntry entry)
    {
        var memberOf = new List<string>();
        if (entry.Attributes.Contains("memberOf"))
        {
            foreach (var value in entry.Attributes["memberOf"].GetValues(typeof(string)))
            {
                memberOf.Add(value.ToString()!);
            }
        }
        return memberOf;
    }

    /// <summary>
    /// Erstellt Claims basierend auf den Gruppenmitgliedschaften und Attributen des Benutzers
    /// </summary>
    private List<Claim> GetUserClaims(AdUserInfo userInfo, LdapConnection connection)
    {
        var claims = new List<Claim>();

        // Standard-Claims basierend auf Benutzerattributen hinzufügen
        if (!string.IsNullOrEmpty(userInfo.DisplayName))
            claims.Add(new Claim(ClaimTypes.Name, userInfo.DisplayName));

        if (!string.IsNullOrEmpty(userInfo.Email))
            claims.Add(new Claim(ClaimTypes.Email, userInfo.Email));

        if (!string.IsNullOrEmpty(userInfo.SamAccountName))
            claims.Add(new Claim(ClaimTypes.WindowsAccountName, userInfo.SamAccountName));

        // Auch vererbte Gruppen ermitteln (rekursiv)
        var allGroups = new HashSet<string>(userInfo.MemberOf);
        foreach (var groupDn in userInfo.MemberOf.ToList())
        {
            GetNestedGroups(groupDn, connection, allGroups);
        }

        // Für jede Gruppe einen Role-Claim erstellen
        foreach (var groupDn in allGroups)
        {
            // Gruppenname aus DN extrahieren (CN=Gruppenname,OU=...)
            string groupName = ExtractGroupNameFromDN(groupDn);
            if (!string.IsNullOrEmpty(groupName))
            {
                claims.Add(new Claim(ClaimTypes.Role, groupName));
            }
        }

        return claims;
    }

    /// <summary>
    /// Ermittelt rekursiv alle übergeordneten Gruppen, in denen eine Gruppe Mitglied ist
    /// </summary>
    private void GetNestedGroups(string groupDn, LdapConnection connection, HashSet<string> allGroups)
    {
        try
        {
            // LDAP-Suchanfrage für die Gruppe erstellen
            SearchRequest searchRequest = new SearchRequest(
                groupDn,
                "(objectClass=*)",
                SearchScope.Base,
                [ "memberOf" ]
            );

            // Suche ausführen
            SearchResponse response = (SearchResponse)connection.SendRequest(searchRequest);

            if (response.Entries.Count > 0)
            {
                SearchResultEntry entry = response.Entries[0];
                var parentGroups = GetMemberOfValues(entry);

                foreach (var parentGroup in parentGroups)
                {
                    if (allGroups.Add(parentGroup)) // Wenn die Gruppe noch nicht in der Liste ist
                    {
                        // Rekursiv weiter nach übergeordneten Gruppen suchen
                        GetNestedGroups(parentGroup, connection, allGroups);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehler beim Ermitteln von verschachtelten Gruppen für {GroupDn}", groupDn);
        }
    }

    /// <summary>
    /// Extrahiert den Gruppennamen aus einem Distinguished Name
    /// </summary>
    private static string ExtractGroupNameFromDN(string dn)
    {
        try
        {
            // CN=Gruppenname,OU=... -> Gruppenname
            if (dn.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                int startIndex = 3; // Nach "CN="
                int endIndex = dn.IndexOf(',', startIndex);
                if (endIndex > startIndex)
                {
                    return dn.Substring(startIndex, endIndex - startIndex);
                }
            }
            return dn;
        }
        catch
        {
            return dn;
        }
    }

}
