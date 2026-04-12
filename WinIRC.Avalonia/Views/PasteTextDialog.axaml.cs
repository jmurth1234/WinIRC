using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WinIRC.Avalonia;

public enum PasteTextAction
{
    Cancel,
    Upload,
    Insert
}

public sealed record PasteTextResult(PasteTextAction Action, string Text);

public partial class PasteTextDialog : Window
{
    public PasteTextDialog()
    {
        InitializeComponent();
    }

    public PasteTextDialog(string text) : this()
    {
        PasteBox.Text = text;
    }

    private void Upload_Click(object? sender, RoutedEventArgs e)
    {
        Close(new PasteTextResult(PasteTextAction.Upload, PasteBox.Text ?? ""));
    }

    private void Insert_Click(object? sender, RoutedEventArgs e)
    {
        Close(new PasteTextResult(PasteTextAction.Insert, PasteBox.Text ?? ""));
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(new PasteTextResult(PasteTextAction.Cancel, ""));
    }
}
