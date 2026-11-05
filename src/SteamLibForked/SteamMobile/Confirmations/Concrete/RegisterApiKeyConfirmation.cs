namespace SteamLib.SteamMobile.Confirmations.Concrete;

public class RegisterApiKeyConfirmation(long id, ulong nonce, ulong creatorId, string typeName)
    : Confirmation(id, nonce, 8, creatorId, ConfirmationType.RegisterApiKey, typeName);