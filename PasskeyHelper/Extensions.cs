using Fido2NetLib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PasskeyHelper.Data;
using PasskeyHelper.Handlers;
using PasskeyHelper.Models.Passkey;
using PasskeyHelper.Models.VerificationMail;
using PasskeyHelper.Pages.Passkey;

namespace PasskeyHelper;

public static class Extensions
{
    public static IServiceCollection AddPasskeyHelper(this IServiceCollection services,
        string connectionString,
        Action<Fido2Configuration> fidoOptions,
        Func<SmtpSettings> smtpOptions,
        Action<SessionOptions>? sessionOptions = null)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        ArgumentNullException.ThrowIfNull(fidoOptions, nameof(Fido2Configuration));
        ArgumentNullException.ThrowIfNull(smtpOptions, nameof(SmtpSettings));

        services.AddSession(sessionOptions ?? (options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
        }));

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddSignInManager<SignInManager<ApplicationUser>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services
            .AddAuthentication(Constants.Common.Identity_ApplicationNamespace)
            .AddCookie(Constants.Common.Identity_ApplicationNamespace, options =>
            {
                options.LoginPath = Constants.PageRoutes.Login;
                options.AccessDeniedPath = Constants.PageRoutes.PasskeyRedirectToLogin;
            });

        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();

        services.AddScoped<Fido2RegisterHandler>();
        services.AddScoped<Fido2LoginHandler>();
        services.AddScoped<AttestationStateService>();
        services.AddScoped<CustomAuthenticationStateProvider>();
        services.AddSingleton(() => smtpOptions.Invoke());
        services.AddFido2(fidoOptions);
        services.AddHttpClient();
        services.AddHttpContextAccessor();

        return services;
    }

    public static WebApplication ConfigurePasskey(this WebApplication app)
    {
        app.UseAntiforgery();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSession();
        InitializeDatabase(app.Services);
        return app;
    }

    public static RazorComponentsEndpointConventionBuilder AddPasskeyPages(this RazorComponentsEndpointConventionBuilder builder)
    {
        builder.AddAdditionalAssemblies(typeof(PasskeyRegister).Assembly);
        return builder;
    }

    private static void InitializeDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        context.Database.Migrate();
    }

}
