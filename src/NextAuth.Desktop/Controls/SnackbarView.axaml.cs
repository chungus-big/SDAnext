using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using NextAuth.Core.Services;

namespace NextAuth.Desktop.Controls;

public partial class SnackbarView : UserControl
{
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<SnackbarView, string?>(nameof(Message));

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<SnackbarView, string?>(nameof(ActionText));

    public static readonly StyledProperty<SnackbarType> TypeProperty =
        AvaloniaProperty.Register<SnackbarView, SnackbarType>(nameof(Type), SnackbarType.Info);

    private CancellationTokenSource? _hideCts;
    private Action? _action;

    static SnackbarView()
    {
        TypeProperty.Changed.AddClassHandler<SnackbarView>((s, e) =>
        {
            if (e.NewValue is SnackbarType newType)
            {
                s.UpdateClasses(newType);
            }
        });
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public SnackbarType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public SnackbarView()
    {
        InitializeComponent();
        ActionButton.Click += ActionButton_OnClick;
        CloseButton.Click += (_, _) => Hide();
        UpdateClasses(Type);
    }

    private void UpdateClasses(SnackbarType type)
    {
        Classes.Set("info", type == SnackbarType.Info);
        Classes.Set("success", type == SnackbarType.Success);
        Classes.Set("warning", type == SnackbarType.Warning);
        Classes.Set("error", type == SnackbarType.Error);
    }

    public void Show(string message, SnackbarType type = SnackbarType.Info, string? actionText = null,
        Action? action = null, TimeSpan? duration = null)
    {
        _hideCts?.Cancel();
        _hideCts?.Dispose();
        _hideCts = new CancellationTokenSource();
        var cts = _hideCts;

        Dispatcher.UIThread.Post(() =>
        {
            Type = type;
            Message = message;
            ActionText = actionText;
            _action = action;
            ActionButton.IsVisible = actionText != null;
            IsVisible = true;
            _ = AutoHideAsync(duration ?? TimeSpan.FromSeconds(3), cts);
        });
    }

    public void Show(string message, string? actionText, Action? action = null, TimeSpan? duration = null)
    {
        Show(message, SnackbarType.Info, actionText, action, duration);
    }

    public void Hide()
    {
        _hideCts?.Cancel();
        _hideCts?.Dispose();
        _hideCts = null;
        Dispatcher.UIThread.Post(() =>
        {
            IsVisible = false;
            ActionButton.IsVisible = false;
        });
    }

    private async Task AutoHideAsync(TimeSpan duration, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(duration, cts.Token);
            Dispatcher.UIThread.Post(() =>
            {
                if (ReferenceEquals(_hideCts, cts))
                {
                    IsVisible = false;
                    ActionButton.IsVisible = false;
                }
            });
        }
        catch (TaskCanceledException)
        {
            // Replaced by new snackbar
        }
    }

    private void ActionButton_OnClick(object? sender, EventArgs e)
    {
        Hide();
        _action?.Invoke();
    }
}
