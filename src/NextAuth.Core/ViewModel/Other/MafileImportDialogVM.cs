using CommunityToolkit.Mvvm.ComponentModel;

namespace NextAuth.Core.ViewModel.Other;

public partial class MafileImportDialogVM(int totalCount, int conflictCount) : ObservableObject
{
    public LocalizationSource Loc => LocalizationSource.Instance;

    public int TotalCount { get; } = totalCount;
    public int ConflictCount { get; } = conflictCount;
    public bool HasConflicts => ConflictCount > 0;
    [ObservableProperty] private bool _overwriteConflicts;
}

public record MafileImportDialogResult(bool OverwriteConflicts);