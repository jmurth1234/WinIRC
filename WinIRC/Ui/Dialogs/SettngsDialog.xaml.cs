using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Handlers;
using WinIRC.Net;
using WinIRC.Ui;
using WinIRC.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsDialog : ContentDialog
    {
        public Type Type { get; private set; }
        public Action UpdateUi { get; internal set; }

        public SettingsDialog(Type type)
        {
            this.InitializeComponent();

            this.Type = type;
            this.Loaded += ConnectView_Loaded;
        }

        private void ConnectView_Loaded(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(Type);

            if (Frame.Content is BaseSettingsPage)
            {
                var settingsView = (BaseSettingsPage)Frame.Content;
                settingsView.UpdateUi += UpdateUi;

                if (settingsView != null)
                {
                    Header.Text = settingsView.Title;
                }
            }
            else if (this.Type.Name == nameof(AboutView))
            {
                Header.Text = "About";
            }
        }
    }
}
