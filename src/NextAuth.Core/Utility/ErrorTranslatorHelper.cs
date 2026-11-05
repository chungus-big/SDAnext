using SteamLib.Core.StatusCodes;
using SteamLib.Exceptions.Authorization;
using SteamLib.Exceptions.Mobile;

namespace NextAuth.Core.Utility;

public static class ErrorTranslatorHelper
{
    public static string TranslateLoginError(LoginError error)
    {
        var result = GetMessage("Login", error.ToString());
        return result ?? error.ToString();
    }

    public static string TranslateSteamStatusCode(SteamStatusCode statusCode)
    {
        var result = GetMessage("EResult", statusCode.Name);
        return result ?? statusCode.ToString();
    }

    public static string TranslateLinkerError(AuthenticatorLinkerError error)
    {
        return GetMessage("AuthenticatorLinkerError", error.ToString()) ?? error.ToString();
    }

    public static string? GetMessage(string path, string name)
    {
        return LocManager.GetCodeBehind("ErrorTranslator", path, name);
    }
}
