using System.ComponentModel;
using System.Runtime.CompilerServices;
using SteamLib.SteamMobile.Confirmations.Details;

namespace SteamLib.SteamMobile.Confirmations.Concrete;

public class Confirmation(long id, ulong nonce, int intType, ulong creatorId, ConfirmationType confType, string typeName) : INotifyPropertyChanged
{
    public long Id { get; } = id;

    /// <summary>
    ///     The unique key used to act upon this confirmation.
    /// </summary>
    public ulong Nonce { get; } = nonce;

    /// <summary>
    ///     Represents either the Trade Offer ID or market transaction ID that caused this confirmation to be created.
    /// </summary>
    public ulong CreatorId { get; } = creatorId;

    public string TypeName { get; init; } = !string.IsNullOrWhiteSpace(typeName) ? typeName : DefaultTypeName(confType);

    private string _headLine = string.Empty;
    public string HeadLine
    {
        get => _headLine;
        set
        {
            if (_headLine != value)
            {
                _headLine = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime Time { get; set; }

    public virtual ConfirmationDetails? Details { get; }

    /// <summary>
    ///     The type of this confirmation.
    /// </summary>
    public readonly ConfirmationType ConfType = confType;

    /// <summary>
    ///     The value of the data-type HTML attribute returned for this contribution.
    /// </summary>
    public int IntType = intType;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static string DefaultTypeName(ConfirmationType type) => type switch
    {
        ConfirmationType.Trade => "Trade Offer",
        ConfirmationType.MarketSellTransaction => "Market Listing",
        ConfirmationType.AccountRecovery => "Account Recovery",
        ConfirmationType.RegisterApiKey => "API Key Registration",
        ConfirmationType.Purchase => "Purchase",
        _ => "Confirmation"
    };
}