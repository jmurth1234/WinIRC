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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace WinIRC.Ui
{
    public sealed partial class HeaderBlock : UserControl
    {
        internal static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(HeaderBlock), new PropertyMetadata(null));

        internal static readonly DependencyProperty ShowBackButtonProperty =
            DependencyProperty.Register("ShowBackButton", typeof(bool), typeof(HeaderBlock), new PropertyMetadata(null));

        public event EventHandler BackButtonClicked;


        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public bool ShowBackButton
        {
            get { return (bool)GetValue(ShowBackButtonProperty); }
            set {
                SetValue(ShowBackButtonProperty, value);
                HandleBackButton();
            }
        }


        public HeaderBlock()
        {
            this.InitializeComponent();
            (this.Content as FrameworkElement).DataContext = this;

            this.Loaded += HeaderBlock_Loaded;
        }

        private void HeaderBlock_Loaded(object sender, RoutedEventArgs e)
        {
            HandleBackButton();
        }

        private void HandleBackButton()
        {
            if (this.ShowBackButton)
            {
                BackButton.Visibility = Visibility.Visible;
            }
            else
            {
                BackButton.Visibility = Visibility.Collapsed;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackButtonClicked?.Invoke(this, new EventArgs());
        }
    }

}
