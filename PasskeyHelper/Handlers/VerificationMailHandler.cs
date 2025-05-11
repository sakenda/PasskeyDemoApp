using Fido2NetLib;
using Microsoft.Extensions.Logging;
using PasskeyHelper.Models.VerificationMail;
using System.Net.Mail;

namespace PasskeyHelper.Handlers;

public class VerificationMailHandler
{
    private readonly ILogger<VerificationMailHandler> _logger;
    private readonly SmtpSettings _smtpSettings;
    private readonly Fido2Configuration _fido2Configuration;

    public VerificationMailHandler(ILogger<VerificationMailHandler> logger, SmtpSettings smtpSettings, Fido2Configuration fido2Configuration)
    {
        _logger = logger;
        _smtpSettings = smtpSettings;
        _fido2Configuration = fido2Configuration;
    }

    internal async Task SendVerificationMailAsync(string token)
    {
        if (OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException("Sending emails is not supported in Blazor WebAssembly.");

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_smtpSettings.MailSettings.From),
            Subject = _smtpSettings.MailSettings.Subject,
            Body = GenerateEmailBody(token),
            IsBodyHtml = true,
            To =
            {
                new MailAddress(_smtpSettings.MailSettings.To)
            }
        };
        
        using var smtpClient = new SmtpClient
        {
            Host = _smtpSettings.Host,
            Port = _smtpSettings.Port,
            Credentials = new System.Net.NetworkCredential(_smtpSettings.Username, _smtpSettings.Password)
        };

        try
        {
            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email.");
            throw;
        }
    }

    private string GenerateEmailBody(string token)
    {
        var origin = _fido2Configuration.Origins.FirstOrDefault();
        var verificationEndpoint = Constants.PageRoutes.VerificationLink;
        var parameters = $"?email={_smtpSettings.MailSettings.To}&token={token}";
        var verificationLink = $"{origin}{verificationEndpoint}{parameters}";

        var header = "Passkey bestätigung";
        var footer = "Wenn du kein Passkey erstellt hast, kannst du diese Email ignorieren.";

        return
            $"<h1>{header}</h1><br />" +
            $"<br />" +
            $"{_smtpSettings.MailSettings.Body}<br />" +
            $"<br />" +
            $"<a href='{verificationLink}'>Registrierung abschließen</a><br />" +
            $"<br />" +
            $"<p>{footer}</p>";
    }
}
