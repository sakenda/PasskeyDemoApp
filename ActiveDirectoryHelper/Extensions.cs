using ActiveDirectoryHelper.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ActiveDirectoryHelper;

public static class Extensions
{
    public static IServiceCollection AddAdLdapService(this IServiceCollection services, IConfiguration configuration)
    {
        var ldapConfig = new LdapConfiguration();
        configuration.GetSection(nameof(LdapConfiguration)).Bind(ldapConfig);

        services.AddSingleton(ldapConfig);
        services.AddScoped<IAdLdapService, AdLdapService>();

        return services;
    }

}
