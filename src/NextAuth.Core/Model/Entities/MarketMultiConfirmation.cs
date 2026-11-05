using System.Collections.ObjectModel;
using SteamLib.SteamMobile.Confirmations;
using SteamLib.SteamMobile.Confirmations.Concrete;

namespace NextAuth.Core.Model.Entities;

public class MarketMultiConfirmation : Confirmation
{
    public ObservableCollection<MarketConfirmation> Confirmations { get; }

    public MarketMultiConfirmation(IEnumerable<MarketConfirmation> confirmations)
        : base(0, 0, 3, 0, ConfirmationType.MarketSellTransaction, "Market Listings")
    {
        Confirmations = [.. confirmations];
        Time = Confirmations.FirstOrDefault()?.Time ?? default;
        UpdateHeadline();
    }

    public void UpdateHeadline()
    {
        var count = Confirmations.Count;
        var template = LocManager.Get("MainWindow", "ConfirmationTemplates", "ItemsCount") ?? "{0} items";
        HeadLine = string.Format(template, count);
    }
}