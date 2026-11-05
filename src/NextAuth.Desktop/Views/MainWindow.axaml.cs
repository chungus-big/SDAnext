using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Services;
using NextAuth.Desktop.Services;
using NextAuth.Core.ViewModel;

namespace NextAuth.Desktop.Views;

public partial class MainWindow : Window
{
    private MainVM? _vm;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void InitializeServices()
    {
        AvaloniaDialogService.SetDialogHost(DialogHostControl);
        AppServices.Dialog = new AvaloniaDialogService();
        AppServices.Clipboard = new AvaloniaClipboardService(Clipboard, StorageProvider);
        AppServices.FileDialog = new AvaloniaFileDialogService(this);
        AppServices.Snackbar = new AvaloniaSnackbarService(Snackbar);
        AppServices.App = new AvaloniaAppService(() => this);
    }

    public void BindViewModel()
    {
        _vm = new MainVM();
        DataContext = _vm;
        Closed += (_, _) => _vm?.Dispose();
        KeyDown += OnWindowKeyDown;
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Formats.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (_vm == null) return;
        var files = e.DataTransfer.TryGetFiles();
        if (files == null) return;

        var paths = files
            .Select(f => f.TryGetLocalPath() ?? (f.Path.IsAbsoluteUri ? f.Path.LocalPath : f.Path.ToString()))
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        if (paths.Length > 0)
        {
            await _vm.AddMafile(paths);
            e.Handled = true;
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        var isCtrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (isCtrlOrCmd && e.Key == Key.V)
        {
            _vm?.PasteMafilesFromClipboardCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void CodeBox_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _vm?.CopyCodeCommand.Execute(null);
    }

    private void LinksText_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _vm?.OpenLinksViewCommand.Execute(null);
    }

    private void MafileListBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Return or Key.Apps ||
            (e.Key == Key.F10 && e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
        {
            OpenSelectedMafileContextMenu();
            e.Handled = true;
        }
    }

    private void MafileItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is Mafile mafile)
        {
            MafileListBox.SelectedItem = mafile;
        }
    }

    private void OpenSelectedMafileContextMenu()
    {
        if (MafileListBox.SelectedItem is not Mafile selectedMafile)
            return;

        var container = MafileListBox.ContainerFromItem(selectedMafile);
        if (container == null)
        {
            var index = MafileListBox.SelectedIndex;
            if (index >= 0)
                container = MafileListBox.ContainerFromIndex(index);
        }

        if (container == null) return;

        var menuHost = container.GetSelfAndVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(c => c.ContextMenu != null);

        if (menuHost?.ContextMenu != null)
        {
            menuHost.ContextMenu.Open(menuHost);
        }
    }
}