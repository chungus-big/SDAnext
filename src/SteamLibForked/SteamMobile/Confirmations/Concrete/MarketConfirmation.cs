namespace SteamLib.SteamMobile.Confirmations.Concrete;

public class MarketConfirmation(long id, ulong key, ulong creator, string typeName)
    : Confirmation(id, key, 3, creator, ConfirmationType.MarketSellTransaction, typeName)
{
    public string ItemImageUri { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string PriceString { get; init; } = string.Empty;
}