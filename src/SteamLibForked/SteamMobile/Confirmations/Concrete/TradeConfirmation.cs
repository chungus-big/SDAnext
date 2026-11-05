using SteamLib.SteamMobile.Confirmations.Details;

namespace SteamLib.SteamMobile.Confirmations.Concrete;

public class TradeConfirmation(long id, ulong nonce, ulong creator, string typeName)
    : Confirmation(id, nonce, 1, creator, ConfirmationType.Trade, typeName)
{
    public string UserAvatarUri { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsReceiveNothing { get; set; }
    public override TradeConfirmationDetails? Details { get; }

    public ulong TradeId => CreatorId;
}