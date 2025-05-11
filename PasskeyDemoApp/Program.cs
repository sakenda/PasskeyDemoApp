using PasskeyDemoApp.Components;
using Microsoft.EntityFrameworkCore;
using PasskeyHelper;
using PasskeyHelper.Models.VerificationMail;

namespace PasskeyDemoApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                options.DetailedErrors = true;
            });
        builder.Services.AddRazorPages();


        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddPasskeyHelper(
            builder.Configuration.GetConnectionString("PasskeyConnection")!,
            options =>
            {
                options.Origins = builder.Configuration.GetSection("Fido2:Origins").Get<HashSet<string>>();
                options.ServerDomain = builder.Configuration["Fido2:ServerDomain"];
                options.ServerName = builder.Configuration["Fido2:ServerName"];
                options.TimestampDriftTolerance = builder.Configuration.GetValue<int>("Fido2:TimestampDriftTolerance");
            },
            () => new SmtpSettings
            {
                Host = builder.Configuration["Smtp:Host"]!,
                Port = builder.Configuration.GetValue<int>("Smtp:Port"),
                Username = builder.Configuration["Smtp:Username"]!,
                Password = builder.Configuration["Smtp:Password"]!,
                MailSettings = new MailSettings
                {
                    From = builder.Configuration["Smtp:MailSettings:From"]!,
                    Subject = builder.Configuration["Smtp:MailSettings:Subject"]!,
                }
            });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.ConfigurePasskey();

        app.MapRazorComponents<App>()
            .AddPasskeyPages()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
