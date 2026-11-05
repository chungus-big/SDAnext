using System.Net;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamLib;
using SteamLib.Models.Session;
using SteamLib.Web;

namespace NextAuth.Core.Model.Entities;

[INotifyPropertyChanged]
public partial class Mafile : MobileDataExtended
{
    public string? Group { get; set; }
    public string? Password { get; set; }

    [JsonIgnore]
    public string? Filename
    {
        get => _filename;
        set => SetProperty(ref _filename, value);
    }

    private string? _filename;

    public void SetSessionData(MobileSessionData? sessionData)
    {
        SessionData = sessionData;
        OnPropertyChanged(nameof(SessionData));
    }

    public static Mafile FromMobileDataExtended(MobileDataExtended data)
    {
        return new Mafile
        {
            AccountName = data.AccountName,
            DeviceId = data.DeviceId,
            IdentitySecret = data.IdentitySecret,
            RevocationCode = data.RevocationCode,
            Secret1 = data.Secret1,
            SerialNumber = data.SerialNumber,
            SessionData = data.SessionData,
            SharedSecret = data.SharedSecret,
            ServerTime = data.ServerTime,
            TokenGid = data.TokenGid,
            Uri = data.Uri,
            SteamId = data.SteamId
        };
    }

    public static Mafile FromMobileDataExtended(MobileDataExtended data, string? group, string? password)
    {
        var result = FromMobileDataExtended(data);
        result.Group = group;
        result.Password = password;
        return result;
    }

    /// <summary>
    /// Creates a <see cref="SteamSessionContext"/> encapsulating this maFile's tokens, secrets, and session cookies.
    /// </summary>
    public SteamSessionContext ToSessionContext(CookieContainer? container = null, HttpClient? client = null)
    {
        return SteamSessionContext.FromMobile(this, SteamId, container, client);
    }

    public override string ToString() =>
        $"Mafile {{ AccountName = {AccountName}, SteamId = {SteamId}, Group = {Group}, Filename = {Filename}, DeviceId = {DeviceId}, Password = ****, SharedSecret = ****, IdentitySecret = ****, RevocationCode = **** }}";
}