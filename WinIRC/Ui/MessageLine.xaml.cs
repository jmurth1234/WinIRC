using IrcClientCore;
using OpenGraph_Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using WinIRC.Views;
using WinIRC.Views.InlineViewers;

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

        public static readonly DependencyProperty CompactModeProperty =
            DependencyProperty.Register(
                "CompactMode",
                typeof(bool),
                typeof(MessageLine),
                new PropertyMetadata(null));

        private HyperlinkManager hyperlinkManager;
        private Uri lastUri;

        public Message MessageItem
        {
            get { return (Message)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public bool CompactMode
        {
            get
            {
                return (bool) GetValue(CompactModeProperty);
            }
            set
            {
                SetValue(CompactModeProperty, value);
            }
        }

        public bool HasLoaded { get; private set; }

        public MessageLine()
        {
            this.InitializeComponent();
            this.hyperlinkManager = new HyperlinkManager();

            Loaded += MessageLine_Loaded;
            Unloaded += MessageLine_Unloaded;
        }

        private void MessageLine_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContext = null;
            PreviewFrame.Navigate(typeof(Page));

            hyperlinkManager.SetText(MessageParagraph, "");
            hyperlinkManager.LinkClicked -= MediaPreview_Clicked;
            hyperlinkManager = null;

            UpdateLayout();
        }

        private async void MessageLine_Loaded(object sender, RoutedEventArgs e)
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

            if (MessageItem != null)
            {
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

                hyperlinkManager.SetText(MessageParagraph, MessageItem.Text);
                hyperlinkManager.LinkClicked += MediaPreview_Clicked;
            }

            try
            {
                if (!hyperlinkManager.InlineLink && hyperlinkManager.FirstLink != null && Config.GetBoolean(Config.ShowMetadata, true))
                {
                    var graph = await OpenGraph.ParseUrlAsync(hyperlinkManager.FirstLink);

                    if (graph.Values.Count > 0 && graph.Title != "" && graph["description"] != "")
                    {
                        PreviewFrame.Visibility = Visibility.Visible;
                        PreviewFrame.Navigate(typeof(LinkView), graph);
                    }
                }
            }
            catch { } // swallow exceptions

            this.HasLoaded = true;
        }

        private void MediaPreview_Clicked(Uri uri)
        {
            if (PreviewFrame.Visibility == Visibility.Collapsed)
            {
                PreviewFrame.Visibility = Visibility.Visible;

                if (uri != lastUri)
                {
                    if (uri.Host.Contains("twitter.com"))
                        PreviewFrame.Navigate(typeof(TwitterView), uri);
                    else if (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be"))
                        PreviewFrame.Navigate(typeof(YoutubeView), uri);
                    else if (HyperlinkManager.isImage(uri.ToString()))
                        PreviewFrame.Navigate(typeof(ImageView), uri);

                }

                lastUri = uri;
            }
            else
            {
                PreviewFrame.Visibility = Visibility.Collapsed;
            }
        }
    }
}
