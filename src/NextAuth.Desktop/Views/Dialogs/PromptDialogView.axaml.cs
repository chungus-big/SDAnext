using NextAuth.Desktop.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DialogHostAvalonia;

namespace NextAuth.Desktop.Views.Dialogs;

public partial class PromptDialogView : UserControl
{
    public string? Title
    {
        get => TitleText.Text;
        set => TitleText.Text = value;
    }

    public string? Hint
    {
        get => HintText.Text;
        set => HintText.Text = value;
    }

    public string? Watermark
    {
        get => InputBox.PlaceholderText;
        set
        {
            InputBox.PlaceholderText = value;
            SecretField.PlaceholderText = value;
        }
    }

    public bool IsPassword
    {
        get => !InputBox.IsVisible;
        set
        {
            InputBox.IsVisible = !value;
            SecretField.IsVisible = value;
        }
    }

    public PromptDialogView()
    {
        InitializeComponent();

        OkButton.Click += (_, _) => Submit();
        CancelButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, null);

        InputBox.KeyDown += OnInputKeyDown;
        SecretField.KeyDown += OnInputKeyDown;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Submit();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, null);
            e.Handled = true;
        }
    }

    private void Submit()
    {
        var value = IsPassword ? SecretField.Text : InputBox.Text;
        DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, value ?? string.Empty);
    }

    public void SetDefaultText(string value)
    {
        InputBox.Text = value;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (IsPassword) SecretField.Focus();
        else InputBox.Focus();
    }
}