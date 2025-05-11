namespace PasskeyHelper;

public static class Constants
{
    public static class PageRoutes
    {
        public const string Login = "/login";
        public const string PasskeyLogout = "/passkey/logout";
        public const string PasskeyRedirectToLogin = "/passkey/redirectToLogin";
        public const string PasskeySignInRedirect = "/passkey/signin-redirect";
        public const string VerificationLink = "/verify";
    }

    internal static class Common
    {
        internal const string Fido2AttestationOptionsKey = "fido2.attestationOptions";
        internal const string Identity_ApplicationNamespace = "Identity.Application";
    }

    internal static class JSScriptNames
    {
        internal const string LoginScriptPath = "./_content/PasskeyHelper/js/login.js";
        internal const string RegisterScriptPath = "./_content/PasskeyHelper/js/register.js";
        internal const string FunctionStartAssertion = "startAssertion";
        internal const string FunctionStartAttestation = "startAttestation";
    }
}
