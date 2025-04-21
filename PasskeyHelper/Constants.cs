namespace PasskeyHelper;

public static class Constants
{
    public static class PageRoutes
    {
        public const string PasskeyRegister = "/passkey/register";
        public const string PasskeyLogin = "/passkey/login";
        public const string PasskeyLogout = "/passkey/logout";
        public const string PasskeyRedirectToLogin = "/passkey/redirectToLogin";
        public const string PasskeySignInRedirect = "/passkey/signin-redirect";
    }

    public static class Common
    {
        public const string Fido2AttestationOptionsKey = "fido2.attestationOptions";
        public const string Identity_ApplicationNamespace = "Identity.Application";
    }

    public static class JSScriptNames
    {
        public const string LoginScriptPath = "./_content/PasskeyHelper/js/login.js";
        public const string RegisterScriptPath = "./_content/PasskeyHelper/js/register.js";
        public const string FunctionStartAssertion = "startAssertion";
        public const string FunctionStartAttestation = "startAttestation";
    }
}
