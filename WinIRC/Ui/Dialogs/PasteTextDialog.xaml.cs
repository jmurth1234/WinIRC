using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC.Ui.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PasteTextDialog : ContentDialog
    {
        internal static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("PasteText", typeof(string), typeof(PasteImageDialog), new PropertyMetadata(null));
        private BitmapImage bitmapImage;

        public string PasteText
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public PasteTextDialog(string text)
        {
            this.InitializeComponent();

            Editor.Loading += Editor_Loading;

            this.PasteText = text;
        }

        private void Editor_Loading(object sender, RoutedEventArgs e)
        {
            Editor.Options.Minimap = new Monaco.Editor.IEditorMinimapOptions() {
                Enabled = false
            };
            Editor.Options.FontSize = 10;
            Editor.Options.Folding = false;
        }
    }
}
