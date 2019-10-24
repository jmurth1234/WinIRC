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
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WinIRC.Ui
{
    public sealed partial class PivotPage : UserControl
    {
        public static readonly DependencyProperty ServerProperty =
            DependencyProperty.Register(
                "Server",
                typeof(String),
                typeof(PivotPage),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register(
                "Channel",
                typeof(String),
                typeof(PivotPage),
                new PropertyMetadata(null));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                "Placeholder",
                typeof(bool),
                typeof(PivotPage),
                new PropertyMetadata(null));

        private HyperlinkManager hyperlinkManager;
        private Uri lastUri;

        public String Server
        {
            get => (String) GetValue(ServerProperty); 
            set => SetValue(ServerProperty, value); 
        }

        public String Channel
        {
            get => (String) GetValue(ChannelProperty); 
            set => SetValue(ChannelProperty, value); 
        }

        public bool Placeholder
        {
            get => (bool) GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public PivotPage()
        {
            this.InitializeComponent();
            this.Loaded += PivotPage_Loaded;
        }

        private void PivotPage_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
