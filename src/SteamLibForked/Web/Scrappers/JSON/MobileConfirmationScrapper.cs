using System.Text.Json;
using SteamLib.Exceptions;
using SteamLib.Exceptions.Authorization;
using SteamLib.Exceptions.General;
using SteamLib.SteamMobile.Confirmations;
using SteamLib.SteamMobile.Confirmations.Concrete;
using SteamLib.Web.Models.Mobile;
using SteamLib.Web.Serialization;

namespace SteamLib.Web.Scrappers.JSON;

public static class MobileConfirmationScrapper
{
    public static Dictionary<string, LoadConfirmationsError> ErrorMessages { get; } = new()
    {
        {"Oh nooooooes!", LoadConfirmationsError.TryAgainLater},
        {"You are not set up to receive mobile confirmations", LoadConfirmationsError.NotSetupToReceiveConfirmations}
    };

    /// <summary>
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    /// <exception cref="UnsupportedResponseException"></exception>
    /// <exception cref="SessionInvalidException"></exception>
    /// <exception cref="CantLoadConfirmationsException"></exception>
    public static List<Confirmation> Scrap(string response)
    {
        ConfirmationsJson conf;
        try
        {
            conf = JsonSerializer.Deserialize(response, SteamJsonSerializerContext.Default.ConfirmationsJson)
                   ?? throw new UnsupportedResponseException(response, "Deserialized ConfirmationsJson was null");
        }
        catch (UnsupportedResponseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new UnsupportedResponseException(response, ex);
        }

        if (conf.NeedAuth)
        {
            throw new SessionInvalidException();
        }

        if (!conf.Success)
        {
            var error = LoadConfirmationsError.Unknown;
            if (conf.Message != null && ErrorMessages.TryGetValue(conf.Message, out var e))
            {
                error = e;
            }

            throw new CantLoadConfirmationsException("Error while loading confirmations")
            {
                Error = error,
                ErrorMessage = conf.Message,
                ErrorDetails = conf.Detail
            };
        }

        var result = new List<Confirmation>();
        if (conf.Conf == null || conf.Conf.Count == 0) return result;

        foreach (var confirmationJson in conf.Conf)
        {
            if (confirmationJson == null) continue;
            result.Add(GetConcrete(confirmationJson));
        }

        return result;
    }

    private static Confirmation GetConcrete(ConfirmationJson json)
    {
        return json.Type switch
        {
            ConfirmationType.Trade => GetTradeConfirmation(json),
            ConfirmationType.AccountRecovery => GetAccountRecoveryConfirmation(json),
            ConfirmationType.MarketSellTransaction => GetMarketConfirmation(json),
            ConfirmationType.RegisterApiKey => GetRegisterApiKeyConfirmation(json),
            ConfirmationType.Purchase => GetPurchaseConfirmation(json),
            _ => new Confirmation(json.Id, json.Nonce, (int)json.Type, json.CreatorId, json.Type, json.TypeName)
            {
                Time = json.CreationTime.ToLocalDateTime(),
                HeadLine = json.Headline
            }
        };
    }

    private static TradeConfirmation GetTradeConfirmation(ConfirmationJson json)
    {
        var avatarUri = json.Icon?.ToString() ?? string.Empty;
        var userName = json.Headline;

        var receiveNodeText = json.Summary.Count > 1 ? json.Summary[1] : string.Empty;
        var receiveNothing = receiveNodeText == "You will receive nothing";
        var headline = !string.IsNullOrWhiteSpace(userName)
            ? userName
            : (json.Summary.Count > 0 && !string.IsNullOrWhiteSpace(json.Summary[0]) ? json.Summary[0] : string.Empty);

        return new TradeConfirmation(json.Id, json.Nonce, json.CreatorId, json.TypeName)
        {
            UserAvatarUri = avatarUri,
            UserName = userName,
            IsReceiveNothing = receiveNothing,
            Time = json.CreationTime.ToLocalDateTime(),
            HeadLine = headline
        };
    }

    private static AccountRecoveryConfirmation GetAccountRecoveryConfirmation(ConfirmationJson confirmation)
    {
        return new AccountRecoveryConfirmation(confirmation.Id, confirmation.Nonce, confirmation.CreatorId,
            confirmation.TypeName)
        {
            Time = confirmation.CreationTime.ToLocalDateTime(),
            HeadLine = confirmation.Headline
        };
    }

    private static RegisterApiKeyConfirmation GetRegisterApiKeyConfirmation(ConfirmationJson confirmation)
    {
        return new RegisterApiKeyConfirmation(confirmation.Id, confirmation.Nonce, confirmation.CreatorId,
            confirmation.TypeName)
        {
            Time = confirmation.CreationTime.ToLocalDateTime(),
            HeadLine = confirmation.Headline
        };
    }

    private static MarketConfirmation GetMarketConfirmation(ConfirmationJson confirmation)
    {
        var (itemName, price) = ParseMarketDetails(confirmation.Headline, confirmation.Summary);
        var itemImageUri = confirmation.Icon?.ToString() ?? string.Empty;

        var headline = !string.IsNullOrWhiteSpace(price) && !string.IsNullOrWhiteSpace(itemName) && !itemName.Equals(price, StringComparison.OrdinalIgnoreCase)
            ? $"{itemName} — {price}"
            : (!string.IsNullOrWhiteSpace(itemName) ? itemName : price);

        return new MarketConfirmation(confirmation.Id, confirmation.Nonce, confirmation.CreatorId,
            confirmation.TypeName)
        {
            Time = confirmation.CreationTime.ToLocalDateTime(),
            ItemImageUri = itemImageUri,
            ItemName = itemName,
            PriceString = price,
            HeadLine = headline
        };
    }

    private static readonly string[] SellingForPrefixes =
    [
        "Selling for",
        "Starting at",
        "Продается за",
        "Продаётся за",
        "Начальная цена",
        "Продається за",
        "Початкова ціна",
        "Verkauf für",
        "Verkaufspreis",
        "Ab",
        "Vendu pour",
        "En vente pour",
        "À partir de",
        "A partir de",
        "En venta por",
        "Se vende por",
        "À venda por",
        "Vendido por",
        "In vendita per",
        "Sprzedaż za",
        "Od",
        "Şu fiyata satılıyor:",
        "Başlangıç fiyatı:",
        "Fiyatı:",
        "售价",
        "起价",
        "售價",
        "販売価格",
        "開始価格",
        "판매 가격",
        "시작 가격"
    ];

    public static (string itemName, string price) ParseMarketDetails(string headline, List<string> summary)
    {
        var hl = headline?.Trim() ?? string.Empty;
        var s0 = summary.Count > 0 ? summary[0]?.Trim() ?? string.Empty : string.Empty;
        var s1 = summary.Count > 1 ? summary[1]?.Trim() ?? string.Empty : string.Empty;

        var hlIsPrefix = IsSellingForPrefix(hl);
        var s0IsPrefix = IsSellingForPrefix(s0);

        string itemName;
        string price;

        if (hlIsPrefix)
        {
            price = CleanPricePrefix(hl);
            itemName = !string.IsNullOrWhiteSpace(s0) ? s0 : hl;
        }
        else if (s0IsPrefix)
        {
            price = CleanPricePrefix(s0);
            itemName = !string.IsNullOrWhiteSpace(hl) ? hl : s0;
        }
        else
        {
            var hlIsPrice = LooksLikePrice(hl);
            var s0IsPrice = LooksLikePrice(s0);

            if (hlIsPrice && !s0IsPrice && !string.IsNullOrWhiteSpace(s0))
            {
                price = CleanPricePrefix(hl);
                itemName = s0;
            }
            else if (s0IsPrice && !hlIsPrice && !string.IsNullOrWhiteSpace(hl))
            {
                price = CleanPricePrefix(s0);
                itemName = hl;
            }
            else if (summary.Count > 1 && !string.IsNullOrWhiteSpace(s0))
            {
                // In modern Steam Mobile JSON with multiple summaries, summary[0] is the item name and summary[1] is receive info
                itemName = s0;
                price = CleanPricePrefix(hl);
            }
            else if (!string.IsNullOrWhiteSpace(s0))
            {
                itemName = !string.IsNullOrWhiteSpace(hl) ? hl : s0;
                price = CleanPricePrefix(s0);
            }
            else
            {
                itemName = CleanPricePrefix(hl);
                price = !string.IsNullOrWhiteSpace(s1) ? CleanPricePrefix(s1) : string.Empty;
            }
        }

        if (string.IsNullOrWhiteSpace(price) && !string.IsNullOrWhiteSpace(s1) && LooksLikePrice(s1))
        {
            price = CleanPricePrefix(s1);
        }

        return (itemName, price);
    }

    private static bool IsSellingForPrefix(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var span = text.AsSpan().Trim();
        foreach (var prefix in SellingForPrefixes)
        {
            if (span.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool LooksLikePrice(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim();
        if (IsSellingForPrefix(t)) return true;

        var hasDigit = false;
        var hasCurrencySymbol = false;
        foreach (var c in t)
        {
            if (char.IsDigit(c)) hasDigit = true;
            if (c is '$' or '€' or '£' or '¥' or '₽' or '₹' or '₺' or '₴' or '₸' or '₫' or '฿' or '₩')
                hasCurrencySymbol = true;
        }

        if (!hasDigit) return false;
        if (hasCurrencySymbol) return true;

        return t.Contains("руб", StringComparison.OrdinalIgnoreCase)
               || t.Contains("pуб", StringComparison.OrdinalIgnoreCase)
               || t.Contains("USD", StringComparison.OrdinalIgnoreCase)
               || t.Contains("EUR", StringComparison.OrdinalIgnoreCase)
               || t.Contains("RUB", StringComparison.OrdinalIgnoreCase)
               || t.Contains("TL", StringComparison.OrdinalIgnoreCase)
               || t.Contains("zł", StringComparison.OrdinalIgnoreCase)
               || t.Contains("UAH", StringComparison.OrdinalIgnoreCase)
               || t.Contains("KZT", StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanPricePrefix(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var span = raw.AsSpan().Trim();
        foreach (var prefix in SellingForPrefixes)
        {
            if (span.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                span = span[prefix.Length..].Trim();
                break;
            }
        }

        while (span.Length > 0 && (span[0] == ':' || span[0] == '-' || span[0] == '—'))
        {
            span = span[1..].Trim();
        }

        return span.ToString();
    }

    private static PurchaseConfirmation GetPurchaseConfirmation(ConfirmationJson confirmation)
    {
        return new PurchaseConfirmation(confirmation.Id, confirmation.Nonce,
            confirmation.CreatorId, confirmation.TypeName)
        {
            Time = confirmation.CreationTime.ToLocalDateTime(),
            HeadLine = confirmation.Headline
        };
    }
}