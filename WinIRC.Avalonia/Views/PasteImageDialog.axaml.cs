using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace WinIRC.Avalonia;

public partial class PasteImageDialog : Window
{
    public PasteImageDialog()
    {
        InitializeComponent();
    }

    public PasteImageDialog(Bitmap bitmap) : this()
    {
        PreviewImage.Source = bitmap;
    }

    private void Upload_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
