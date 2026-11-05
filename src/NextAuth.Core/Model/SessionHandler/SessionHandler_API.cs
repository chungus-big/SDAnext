using SteamLib.Web;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Model.Mafiles;
using SteamLib.Api.Mobile;
using SteamLib.Authentication;
using SteamLib.Authentication.LoginV2;
using SteamLib.Exceptions.Authorization;
using SteamLib.Factory.Helpers;
using SteamLib.SteamMobile;
using SteamLib.Models.Session;

namespace NextAuth.Core.Model;

public partial class SessionHandler //API
{
    public static async Task RefreshMobileToken(HttpClientHandlerPair chp, Mafile mafile)
    {
        if (mafile.SessionData is not { RefreshToken.IsExpired: false })
            throw new SessionPermanentlyExpiredException(SessionInvalidException.SESSION_NULL_MSG);

        var mobileToken = await SteamMobileApi.RefreshJwt(chp.Client, mafile.SessionData.RefreshToken.Token,
            mafile.SessionData.SteamId);

        var newToken = SteamTokenHelper.Parse(mobileToken);
        mafile.SessionData.SetMobileToken(newToken);

        //Trigger PropertyChanged event for PortableMaClient handling session updated from MaClient
        mafile.SetSessionData(mafile.SessionData);
        await Storage.UpdateMafileAsync(mafile);
        chp.Handler.CookieContainer.SetSteamMobileCookies(mafile.SessionData);
    }

    public static async Task LoginAgain(HttpClientHandlerPair chp, Mafile mafile, string password, bool savePassword)
    {
        var sgGenerator = new SteamGuardCodeGenerator(mafile.SharedSecret);
        var options = new LoginV2ExecutorOptions(StaticLoginConsumer.Instance, chp.Client)
        {
            AuthProviders = [sgGenerator],
            DeviceDetails = DeviceDetailsDefaultBuilder.GetMobileDefaultDevice(),
            WebsiteId = "Mobile"
        };
        chp.Handler.CookieContainer.ClearMobileSessionCookies();
        var result = await LoginV2Executor.DoLogin(options, mafile.AccountName, password);
        AdmissionHelper.TransferCommunityCookies(chp.Handler.CookieContainer);

        //Triggers PropertyChanged event for PortableMaClient handling session updated from MaClient
        mafile.SetSessionData((MobileSessionData)result);
        if (PHandler.IsPasswordSet)
            mafile.Password = savePassword ? PHandler.Encrypt(password) : null;
        await Storage.UpdateMafileAsync(mafile);
    }
}