using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextAuth.Core.Model;
using NextAuth.Core.Model.Mafiles;

namespace NextAuth.Core.ViewModel.Other;

public partial class SettingsVM : ObservableObject
{
    public Settings Settings => Settings.Instance;

    [ObservableProperty] private string? _password;

    [ObservableProperty] private double _renameMafilesProgress;

    [ObservableProperty] private string? _renameResultText;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyRenameSettingCommand))]
    private bool _useAccountNameAsMafileNamePreview;

    public SettingsVM()
    {
        _useAccountNameAsMafileNamePreview = Settings.UseAccountNameAsMafileName;
    }

    [RelayCommand]
    private void SetPassword()
    {
        Settings.IsPasswordSet = PHandler.SetPassword(Password);
    }

    [RelayCommand(CanExecute = nameof(ApplyRenameSettingCanExecute))]
    private async Task ApplyRenameSetting()
    {
        RenameResultText = null;
        var targetValue = UseAccountNameAsMafileNamePreview;
        if (UseAccountNameAsMafileName == targetValue) return;
        RenameMafilesProgress = 0;
        MafilesBulkRenameResult? result = null;
        try
        {
            result = await Storage.RenameMafiles(targetValue, new Progress<double>(p => RenameMafilesProgress = p));
            UseAccountNameAsMafileName = targetValue;
        }
        catch
        {
        }
        finally
        {
            RenameMafilesProgress = 0;
        }

        if (result == null)
        {
            RenameResultText = GetLoc("Error");
            return;
        }

        if (result.Total == 0) return;

        if (result.NotRenamed == 0)
        {
            var l = GetLoc("AllRenamed");
            RenameResultText = string.Format(l, result.Total, result.BackupFileName);
        }
        else
        {
            var l = GetLoc("PartialSuccess");
            RenameResultText =
                string.Format(l, result.Total, result.Renamed, result.Conflict, result.Errors, result.BackupFileName);
        }

        static string GetLoc(string key)
        {
            return LocManager.GetCodeBehindOrDefault(key, "SettingsVM", "MafileRenaming", key);
        }
    }

    private bool ApplyRenameSettingCanExecute()
    {
        return UseAccountNameAsMafileNamePreview != UseAccountNameAsMafileName;
    }

    #region SettingsProps

    public Dictionary<LocalizationLanguage, string> Languages { get; } = new()
    {
        {LocalizationLanguage.English, "English"},
        {LocalizationLanguage.Russian, "Русский"}
    };

    public LocalizationSource Loc => LocalizationSource.Instance;

    public KeyValuePair<LocalizationLanguage, string> SelectedLanguage
    {
        get => new(Language, Languages.TryGetValue(Language, out var name) ? name : Language.ToString());
        set
        {
            Language = value.Key;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Language));
        }
    }

    public LocalizationLanguage Language
    {
        get => Settings.Language;
        set
        {
            Settings.Language = value;
            LocManager.SetApplicationLocalization(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedLanguage));
        }
    }

    public bool ConfirmMafileImport
    {
        get => Settings.ConfirmMafileImport;
        set => Settings.ConfirmMafileImport = value;
    }

    public bool UseAccountNameAsMafileName
    {
        get => Settings.UseAccountNameAsMafileName;
        set
        {
            Settings.UseAccountNameAsMafileName = value;
            ApplyRenameSettingCommand.NotifyCanExecuteChanged();
        }
    }

    public bool SeparateMarketConfirmations
    {
        get => Settings.SeparateMarketConfirmations;
        set => Settings.SeparateMarketConfirmations = value;
    }

    #endregion
}