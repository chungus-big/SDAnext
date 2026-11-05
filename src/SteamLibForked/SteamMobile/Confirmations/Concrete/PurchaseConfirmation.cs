namespace SteamLib.SteamMobile.Confirmations.Concrete;

public class PurchaseConfirmation(long id, ulong nonce, ulong creatorId, string typeName)
    : Confirmation(id, nonce, 12, creatorId, ConfirmationType.Purchase, typeName);