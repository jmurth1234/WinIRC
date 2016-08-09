using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Net;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace WinIRC.Ui
{
    public sealed partial class MessageLine : UserControl
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                "MessageItem",
                typeof(Message),
                typeof(MessageLine),
                new PropertyMetadata(null));

        public Message MessageItem
        {
            get { return (Message)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }
        public MessageLine()
        {
            this.InitializeComponent();

            Loaded += MessageLine_Loaded;
        }

        private void MessageLine_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = MessageItem;
            UpdateLayout();

            if (double.IsNaN(UsernameBox.ActualWidth) || double.IsNaN(TimestampBox.ActualWidth)) return;

            MessageParagraph.TextIndent = UsernameBox.ActualWidth + TimestampBox.ActualWidth;  

            if (MessageBox.ActualHeight > UsernameBox.ActualHeight)
            {
                Thickness margin = new Thickness(0, -1, 0, 0);
                MessageBox.Margin = margin;
            }

            if (MessageItem.Type == MessageType.Info)
            {
                UsernameBox.Style = (Style)Application.Current.Resources["InfoTextBlockStyle"];
                MessageBox.Style = (Style)Application.Current.Resources["InfoTextRichStyle"];
            }
            else if (MessageItem.Type == MessageType.Action)
            {
                UsernameBox.FontStyle = Windows.UI.Text.FontStyle.Italic;
                MessageBox.FontStyle = Windows.UI.Text.FontStyle.Italic;
            }
            
            if (MessageItem.Mention)
            {
                UsernameBox.Foreground = new SolidColorBrush(Colors.Red);
                MessageBox.Foreground = new SolidColorBrush(Colors.Red);
            }
        }


    }
}
