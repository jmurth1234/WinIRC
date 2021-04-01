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
using Windows.ApplicationModel.DataTransfer;
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
                typeof(MessageGroup),
                typeof(MessageLine),
                new PropertyMetadata(null));

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

                var user = MessageItem.Parent.User ?? "";

                if (user.Contains("*"))
                {
                    return "*";
                }

                if (MessageItem.Parent.Type == MessageType.Normal)
                {
                    return String.Format("{0}", user);
                }
                else if (MessageItem.Parent.Type == MessageType.Notice)
                {
                    return String.Format("->{0}<-", user);
                }
                else
                {
                    return String.Format("* {0}", user);
                }
            }
        }

        public bool HasLoaded { get; private set; }
        public MessageGroup MessageItem
        {
            get { return (MessageGroup)GetValue(MessageProperty); }
            set
            {
                SetValue(MessageProperty, value);
                NotifyPropertyChanged("MessageItem");
                NotifyPropertyChanged("Username");
                NotifyPropertyChanged("UserColor");
                NotifyPropertyChanged("UserColorBrush");
                NotifyPropertyChanged("MessageColor");
                NotifyPropertyChanged("TextIndent");
                NotifyPropertyChanged("NormalMessage");
                UpdateUi();
            }
        }

        public Color MentionRed => ThemeColor(Colors.Red);

        public bool NormalMessage
        {
            get
            {
                return MessageItem.Parent.Type != MessageType.JoinPart;
            }
        }

        public SolidColorBrush UserColorBrush
        {
            get
            {
                if (MessageItem == null) return null;

                return new SolidColorBrush(UserColor);
            }
        }
        public Color UserColor
        {
            get
            {
                if (MessageItem == null) return Colors.White;

                var color = ThemeColor(ColorUtils.GenerateColor(MessageItem.Parent.User));

                if (MessageItem.Parent.Mention)
                {
                    return MentionRed;
                }

                return color;
            }
        }

        public SolidColorBrush MessageColor
        {
            get
            {
                if (MessageItem == null) return null;

                if (MessageItem.Parent.Mention)
                {
                    return new SolidColorBrush(MentionRed);
                }

                Color defaultColor = Config.GetBoolean(Config.DarkTheme, true) ? Colors.White : Colors.Black;

                return new SolidColorBrush(defaultColor);
            }
        }

        public MessageLine() : this(null)
        {
        }

        private Color ThemeColor(Color color)
        {
            if (Config.GetBoolean(Config.DarkTheme, true))
            {
                color = ColorUtils.ChangeColorBrightness(color, 0.2f);
            }
            else
            {
                color = ColorUtils.ChangeColorBrightness(color, -0.4f);
            }

            return color;
        }

        public MessageLine(MessageGroup line)
        {
            this.InitializeComponent();

            this.MessageItem = line;

            Unloaded += MessageLine_Unloaded;
            Loaded += MessageLine_Loaded;
            MainPage.instance.UiUpdated += Instance_UiUpdated;
        }

        private void Instance_UiUpdated(object sender, EventArgs e)
        {
            UpdateUi();

            NotifyPropertyChanged("UserColor");
            NotifyPropertyChanged("MessageColor");
        }

        private void MessageLine_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUi();
        }

        public void UpdateUi()
        {
            if (double.IsNaN(UsernameBox.ActualWidth) || double.IsNaN(TimestampBox.ActualWidth)) return;
            if (MessageItem != null)
            {
                if (MessageItem.Parent.Type == MessageType.Info || MessageItem.Parent.Type == MessageType.JoinPart)
                {
                    UsernameBox.Style = (Style)Application.Current.Resources["InfoTextBlockStyle"];
                }
                else if (MessageItem.Parent.Type == MessageType.Action)
                {
                    UsernameBox.FontStyle = Windows.UI.Text.FontStyle.Italic;
                }

                if (MessageItem.Parent.Mention)
                {
                    UsernameBox.Foreground = new SolidColorBrush(Colors.Red);
                }

                if (MessageItem.Parent.Type == MessageType.MOTD)
                {
                    this.FontFamily = new FontFamily("Consolas");
                }
            }

            this.HasLoaded = true;
            UpdateLayout();
        }

        private void MessageLine_Unloaded(object sender, RoutedEventArgs e)
        {
            MainPage.instance.UiUpdated -= Instance_UiUpdated;
            UpdateLayout();
        }
    }
}
