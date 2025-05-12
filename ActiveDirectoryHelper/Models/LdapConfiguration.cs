namespace ActiveDirectoryHelper.Models;

/// <summary>
/// Konfigurationsoptionen für den LDAP-Service
/// </summary>
public class LdapConfiguration
{
    public string Domain { get; set; } = "";
    public string LdapServer { get; set; } = "";
    public int LdapPort { get; set; } = 389;
    public bool UseSSL { get; set; } = false;
    public string SearchBase { get; set; } = "";
    public string ServiceAccountUsername { get; set; } = "";
    public string ServiceAccountPassword { get; set; } = "";

}

