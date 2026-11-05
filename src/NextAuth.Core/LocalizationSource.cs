using System.ComponentModel;

namespace NextAuth.Core;

public class LocalizationSource : INotifyPropertyChanged
{
    public static LocalizationSource Instance { get; } = new();

    public string CurrentLanguage => LocManager.GetCurrentLanguageCode();

    public LocalizationSource()
    {
        LocManager.CurrentLanguageChanged += (_, _) =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        };
    }

    public string this[string key]
    {
        get
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            return LocManager.Get(key) ?? key;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
