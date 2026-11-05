using System.Globalization;
using Avalonia.Data.Converters;
using NextAuth.Core;
using NextAuth.Core.Model.Entities;
using SteamLib.SteamMobile.Confirmations;
using SteamLib.SteamMobile.Confirmations.Concrete;

namespace NextAuth.Desktop.Converters;

public class ConfirmationTitleConverter : IValueConverter, IMultiValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return GetTitle(value as Confirmation);
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is Confirmation conf)
        {
            return GetTitle(conf);
        }

        return string.Empty;
    }

    public static string GetTitle(Confirmation? conf)
    {
        if (conf == null) return string.Empty;

        if (conf is MarketMultiConfirmation)
        {
            return LocManager.Get("MainWindow", "ConfirmationTemplates", "MarketMulti")
                   ?? (!string.IsNullOrWhiteSpace(conf.TypeName) ? conf.TypeName : "Multiple Listings");
        }

        return conf.ConfType switch
        {
            ConfirmationType.Trade => LocManager.Get("MainWindow", "ConfirmationTemplates", "Trade")
                                      ?? LocManager.Get("MainWindow", "ConfirmationTemplates", "TradeWith")
                                      ?? (!string.IsNullOrWhiteSpace(conf.TypeName) ? conf.TypeName : "Trade Offer"),
            ConfirmationType.MarketSellTransaction => LocManager.Get("MainWindow", "ConfirmationTemplates", "Market")
                                                      ?? (!string.IsNullOrWhiteSpace(conf.TypeName) ? conf.TypeName : "Market Listing"),
            ConfirmationType.AccountRecovery => LocManager.Get("MainWindow", "ConfirmationTemplates", "Recovery")
                                                ?? (!string.IsNullOrWhiteSpace(conf.TypeName) ? conf.TypeName : "Account Recovery"),
            ConfirmationType.RegisterApiKey => LocManager.Get("MainWindow", "ConfirmationTemplates", "ApiKey")
                                               ?? (!string.IsNullOrWhiteSpace(conf.TypeName) ? conf.TypeName : "API Key"),
            ConfirmationType.Purchase => LocManager.Get("MainWindow", "ConfirmationTemplates", "Purchase")
                                         ?? (!string.IsNullOrWhiteSpace(conf.TypeName) ? conf.TypeName : "Purchase"),
            _ => !string.IsNullOrWhiteSpace(conf.TypeName) ? conf.TypeName : "Confirmation"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
