using System.ComponentModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

using NextAuth.Core.Utility;

namespace NextAuth.Core.Model;

public partial class Settings : ObservableObject
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static Settings Instance { get; }

    static Settings()
    {
        if (!File.Exists("settings.json"))
        {
            Instance = new Settings();
            Instance.PropertyChanged += SettingsOnPropertyChanged;
            Instance.Language = LanguageUtility.DetectPreferredLanguage();
            return;
        }

        try
        {
            var json = File.ReadAllText("settings.json");
            var settings = JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? throw new NullReferenceException();
            Instance = settings;
        }
        catch (Exception ex)
        {
            SnackbarController.SendError(LocManager.GetCodeBehindOrDefault("Error when loading settings", "Settings",
                "ErrorWhileLoadingSettings"));
            SnackbarController.SendError(ex.Message);
            Instance = new Settings();
        }

        Instance.PropertyChanged += SettingsOnPropertyChanged;
    }

    private static void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Save();
    }

    public static void Save()
    {
        var json = JsonSerializer.Serialize(Instance, JsonOptions);
        File.WriteAllText("settings.json", json);
    }

    #region Properties

    [ObservableProperty] private bool _isPasswordSet;
    [ObservableProperty] private LocalizationLanguage _language = LocalizationLanguage.English;
    [ObservableProperty] private bool _confirmMafileImport = true;
    [ObservableProperty] private bool _useAccountNameAsMafileName;
    [ObservableProperty] private bool _separateMarketConfirmations;

    #endregion
}