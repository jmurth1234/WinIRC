using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WinIRC.Avalonia;

public partial class TextPromptDialog : Window
{
    public TextPromptDialog()
    {
        InitializeComponent();
    }

    public TextPromptDialog(string title, string prompt, string watermark = "") : this()
    {
        Title = title;
        PromptText.Text = prompt;
        InputBox.PlaceholderText = watermark;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Close(InputBox.Text ?? "");
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close("");
    }
}
