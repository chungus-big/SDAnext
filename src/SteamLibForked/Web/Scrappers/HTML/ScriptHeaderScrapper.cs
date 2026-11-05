using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SteamLib.Exceptions.General;
using SteamLib.Models;

namespace SteamLib.Web.Scrappers.HTML;

/// <summary>
/// Scrapes global JavaScript header variables (<c>g_sessionID</c>, <c>g_steamID</c>)
/// embedded in Steam HTML web pages.
/// </summary>
public static partial class ScriptHeaderScrapper
{
    private const string XPATH = "//div[@class='responsive_page_content']/script";

    [GeneratedRegex(@"g_sessionID = ""(?<g_sessionID>.*)"";\s*g_steamID = (?<g_steamID>.*);", RegexOptions.Compiled)]
    private static partial Regex ScriptHeaderRegex();


    public static ScriptHeaderModel Scrap(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        return Scrap(document);
    }

    public static ScriptHeaderModel Scrap(HtmlDocument document)
    {
        var scriptNode = document.DocumentNode.SelectSingleNode(XPATH) ??
                         throw new NullReferenceException("Script Node was null");
        var script = scriptNode.InnerText;
        return ConvertScript(script);
    }

    public static ScriptHeaderModel ConvertScript(string script)
    {
        var match = ScriptHeaderRegex().Match(script);
        if (!match.Success)
            throw new UnsupportedResponseException(script, "Page contains script but regex was unsuccessful");

        var steamIdStr = match.Groups["g_steamID"].Value.Trim('"', ' ');
        var sessionId = match.Groups["g_sessionID"].Value;
        SteamId? steamId;
        if (steamIdStr == "false") // notLoggedIn
        {
            steamId = null;
        }
        else
        {
            steamId = SteamId.Parse(steamIdStr);
        }

        return new ScriptHeaderModel
        {
            SteamId = steamId,
            SessionId = sessionId
        };
    }
}