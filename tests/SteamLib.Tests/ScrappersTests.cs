using SteamLib.Exceptions;
using SteamLib.Exceptions.Authorization;
using SteamLib.Exceptions.General;
using SteamLib.SteamMobile.Confirmations.Concrete;
using SteamLib.Web.Scrappers.HTML;
using SteamLib.Web.Scrappers.JSON;
using Xunit;

namespace SteamLib.Tests;

public class ScrappersTests
{
    [Fact]
    public void ScriptHeaderScrapper_LoggedInHtml_ScrapsSessionIdAndSteamId()
    {
        var html = """
        <!DOCTYPE html>
        <html>
        <head><title>Steam Community</title></head>
        <body>
            <div class="responsive_page_content">
                <script type="text/javascript">
                    g_sessionID = "a1b2c3d4e5f60718";
                    g_steamID = "76561197960287930";
                </script>
            </div>
        </body>
        </html>
        """;

        var result = ScriptHeaderScrapper.Scrap(html);

        Assert.NotNull(result);
        Assert.Equal("a1b2c3d4e5f60718", result.SessionId);
        Assert.NotNull(result.SteamId);
        Assert.Equal(76561197960287930L, result.SteamId.Value.Steam64.Id);
    }

    [Fact]
    public void ScriptHeaderScrapper_NotLoggedInHtml_ScrapsSessionIdWithNullSteamId()
    {
        var html = """
        <!DOCTYPE html>
        <html>
        <body>
            <div class="responsive_page_content">
                <script type="text/javascript">
                    g_sessionID = "anonymous_session_99";
                    g_steamID = false;
                </script>
            </div>
        </body>
        </html>
        """;

        var result = ScriptHeaderScrapper.Scrap(html);

        Assert.NotNull(result);
        Assert.Equal("anonymous_session_99", result.SessionId);
        Assert.Null(result.SteamId);
    }

    [Fact]
    public void ScriptHeaderScrapper_MalformedScript_ThrowsUnsupportedResponseException()
    {
        var html = """
        <div class="responsive_page_content">
            <script>
                var someOtherVar = 123;
            </script>
        </div>
        """;

        Assert.Throws<UnsupportedResponseException>(() => ScriptHeaderScrapper.Scrap(html));
    }

    [Fact]
    public void MobileConfirmationScrapper_ValidResponse_ParsesTradeAndMarketConfirmations()
    {
        var json = """
        {
          "success": true,
          "conf": [
            {
              "type": 2,
              "type_name": "Trade",
              "id": 1234567890,
              "creator_id": 9876543210,
              "nonce": 1122334455,
              "creation_time": 1700000000,
              "cancel": "Cancel",
              "accept": "Accept",
              "icon": "https://avatars.fastly.steamstatic.com/avatar.jpg",
              "multi": false,
              "headline": "Trade with Player1",
              "summary": ["AK-47 | Vulcan", "You will receive nothing"]
            },
            {
              "type": 3,
              "type_name": "Market",
              "id": 9876543211,
              "creator_id": 9876543210,
              "nonce": 5544332211,
              "creation_time": 1700000050,
              "cancel": "Cancel",
              "accept": "Accept",
              "multi": false,
              "headline": "Listing on Community Market",
              "summary": ["$12.50 (Operation Bravo Case)"]
            }
          ]
        }
        """;

        var confirmations = MobileConfirmationScrapper.Scrap(json);

        Assert.NotNull(confirmations);
        Assert.Equal(2, confirmations.Count);

        var tradeConf = Assert.IsType<TradeConfirmation>(confirmations[0]);
        Assert.Equal(1234567890L, tradeConf.Id);
        Assert.Equal(1122334455UL, tradeConf.Nonce);
        Assert.Equal("Trade with Player1", tradeConf.UserName);
        Assert.True(tradeConf.IsReceiveNothing);

        var marketConf = Assert.IsType<MarketConfirmation>(confirmations[1]);
        Assert.Equal(9876543211L, marketConf.Id);
        Assert.Equal(5544332211UL, marketConf.Nonce);
    }

    [Fact]
    public void MobileConfirmationScrapper_NeedAuth_ThrowsSessionInvalidException()
    {
        var json = """
        {
          "success": false,
          "needauth": true
        }
        """;

        Assert.Throws<SessionInvalidException>(() => MobileConfirmationScrapper.Scrap(json));
    }

    [Fact]
    public void MobileConfirmationScrapper_UnsuccessfulWithMessage_ThrowsCantLoadConfirmationsException()
    {
        var json = """
        {
          "success": false,
          "message": "Oh nooooooes!",
          "detail": "Steam servers are experiencing high load"
        }
        """;

        var ex = Assert.Throws<CantLoadConfirmationsException>(() => MobileConfirmationScrapper.Scrap(json));
        Assert.Equal(LoadConfirmationsError.TryAgainLater, ex.Error);
        Assert.Equal("Oh nooooooes!", ex.ErrorMessage);
    }

    [Fact]
    public void MobileConfirmationScrapper_NullOrEmptyConf_ReturnsEmptyList()
    {
        var jsonWithNullConf = """
        {
          "success": true,
          "conf": null
        }
        """;

        var resultNull = MobileConfirmationScrapper.Scrap(jsonWithNullConf);
        Assert.NotNull(resultNull);
        Assert.Empty(resultNull);

        var jsonWithEmptyConf = """
        {
          "success": true,
          "conf": []
        }
        """;

        var resultEmpty = MobileConfirmationScrapper.Scrap(jsonWithEmptyConf);
        Assert.NotNull(resultEmpty);
        Assert.Empty(resultEmpty);
    }
}
