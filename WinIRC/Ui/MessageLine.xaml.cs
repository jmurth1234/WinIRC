using IrcClientCore;
using OpenGraph_Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using WinIRC.Net;
using WinIRC.Views;
using WinIRC.Views.InlineViewers;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace WinIRC.Ui
{
    public sealed partial class MessageLine : UserControl, INotifyPropertyChanged
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
        private object p;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Username
        {
            get
            {
                if (MessageItem == null) return "";
                if (MessageItem.User.Contains("*"))
                {
                    return "*";
                }

                if (MessageItem.Type == MessageType.Normal)
                {
                    return String.Format("<{0}>", MessageItem.User);
                }
                else if (MessageItem.Type == MessageType.Notice)
                {
                    return String.Format("->{0}<-", MessageItem.User);
                }
                else
                {
                    return String.Format("* {0}", MessageItem.User);
                }
            }
        }

        public bool CompactMode
        {
            get
            {
                return (bool)GetValue(CompactModeProperty);
            }
            set
            {
                SetValue(CompactModeProperty, value);
            }
        }

        public bool HasLoaded { get; private set; }
        public Message MessageItem
        {
            get { return (Message)GetValue(MessageProperty); }
            set
            {
                SetValue(MessageProperty, value);
                NotifyPropertyChanged("MessageItem");
                NotifyPropertyChanged("Username");
                NotifyPropertyChanged("TextIndent");
                UpdateUi();
            }
        }

        public MessageLine() : this(null)
        {
        }

        public MessageLine(Message line)
        {
            this.InitializeComponent();

            this.MessageItem = line;

            Unloaded += MessageLine_Unloaded;
            Loaded += MessageLine_Loaded;
            LayoutUpdated += MessageLine_LayoutUpdated;
            MainPage.instance.UiUpdated += Instance_UiUpdated;
        }

        private void Instance_UiUpdated(object sender, EventArgs e)
        {
            UpdateUi();
        }

        private void MessageLine_LayoutUpdated(object sender, object e)
        {
            var wantedPadding = UsernameBox.ActualWidth + TimestampBox.ActualWidth;
            if (MessageParagraph.TextIndent != wantedPadding)
            {
                MessageParagraph.TextIndent = wantedPadding;
            }
        }

        private void MessageLine_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUi();
        }

        public void UpdateUi()
        {
            InvalidateMeasure();
            UpdateLayout();
            this.hyperlinkManager = new HyperlinkManager();

            if (double.IsNaN(UsernameBox.ActualWidth) || double.IsNaN(TimestampBox.ActualWidth)) return;
            if (MessageItem != null)
            {
                PreviewFrame.Visibility = Visibility.Collapsed;

                if (hyperlinkManager.LinkClicked != null)
                {
                    hyperlinkManager.LinkClicked -= MediaPreview_Clicked;
                }
                if (MessageBox.ActualHeight > UsernameBox.ActualHeight)
                {
                    Thickness margin = new Thickness(0, -1, 0, 0);
                    MessageBox.Margin = margin;
                }

                if (MessageItem.Type == MessageType.Info || MessageItem.Type == MessageType.JoinPart)
                {
                    UsernameBox.Style = (Style)Application.Current.Resources["InfoTextBlockStyle"];
                    MessageBox.Style = (Style)Application.Current.Resources["InfoTextRichStyle"];
                }
                else if (MessageItem.Type == MessageType.Action)
                {
                    UsernameBox.FontStyle = Windows.UI.Text.FontStyle.Italic;
                    MessageBox.FontStyle = Windows.UI.Text.FontStyle.Italic;
                }

                var color = ColorUtils.GenerateColor(MessageItem.User);
                if (Config.GetBoolean(Config.DarkTheme, true))
                {
                    color = ColorUtils.ChangeColorBrightness(color, 0.2f);
                }
                else
                {
                    color = ColorUtils.ChangeColorBrightness(color, -0.4f);
                }
                UsernameBox.Foreground = new SolidColorBrush(color);

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
                    Task.Run(async () =>
                    {
                        var graph = await OpenGraph.ParseUrlAsync(hyperlinkManager.FirstLink);

                        if (graph.Values.Count > 0 && graph.Title != "" && graph["description"] != "")
                        {
                            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                PreviewFrame.Visibility = Visibility.Visible;
                                PreviewFrame.Navigate(typeof(LinkView), graph, new SuppressNavigationTransitionInfo());
                            });
                        }
                    });
                }
            }
            catch { } // swallow exceptions

            this.HasLoaded = true;
            UpdateLayout();
        }

        private void MessageLine_Unloaded(object sender, RoutedEventArgs e)
        {
            PreviewFrame.Navigate(typeof(Page));

            hyperlinkManager.SetText(MessageParagraph, "");
            hyperlinkManager.LinkClicked -= MediaPreview_Clicked;
            hyperlinkManager = null;
            MainPage.instance.UiUpdated -= Instance_UiUpdated;
            UpdateLayout();
        }

        private void MediaPreview_Clicked(Uri uri)
        {
            if (PreviewFrame.Visibility == Visibility.Collapsed)
            {
                PreviewFrame.Visibility = Visibility.Visible;

                if (uri != lastUri)
                {
                    if (uri.Host.Contains("twitter.com"))
                        PreviewFrame.Navigate(typeof(TwitterView), uri, new SuppressNavigationTransitionInfo());
                    else if (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be"))
                        PreviewFrame.Navigate(typeof(YoutubeView), uri, new SuppressNavigationTransitionInfo());
                    else if (HyperlinkManager.isImage(uri.ToString()))
                        PreviewFrame.Navigate(typeof(ImageView), uri, new SuppressNavigationTransitionInfo());
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
