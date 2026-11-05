namespace SteamLib.SteamMobile.Confirmations.Concrete;

public class AccountRecoveryConfirmation(long id, ulong nonce, ulong creator, string typeName)
    : Confirmation(id, nonce, 6, creator, ConfirmationType.AccountRecovery, typeName);