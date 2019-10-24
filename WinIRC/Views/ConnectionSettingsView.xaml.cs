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
using WinIRC.Ui;

namespace WinIRC.Views
{
    public sealed partial class ConnectionSettingsView : BaseSettingsPage
    {
        public ConnectionSettingsView()
        {
            this.InitializeComponent();
            Title = "Connection";
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (Config.Contains(Config.IgnoreSSL))
            {
                this.IgnoreSSLErrors.IsOn = Config.GetBoolean(Config.IgnoreSSL);
            }
            else
            {
                Config.SetBoolean(Config.IgnoreSSL, false);
                this.IgnoreSSLErrors.IsOn = false;
            }

            if (Config.Contains(Config.AutoReconnect))
            {
                this.ReconnectSwitch.IsOn = Config.GetBoolean(Config.AutoReconnect);
            }
            else
            {
                Config.SetBoolean(Config.AutoReconnect, true);
                this.ReconnectSwitch.IsOn = true;
            }

            DefaultUsername.Text = Config.GetString(Config.DefaultUsername);

            this.SettingsLoaded = true;
        }


        private void ReconnectSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;

            Config.SetBoolean(Config.AutoReconnect, ReconnectSwitch.IsOn);
        }

        private void IgnoreSSLErrors_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;

            Config.SetBoolean(Config.IgnoreSSL, IgnoreSSLErrors.IsOn);
        }

        private void DefaultUsername_LostFocus(object sender, RoutedEventArgs e)
        {
            SetUsername();
        }

        private void DefaultUsernameButton_Click(object sender, RoutedEventArgs e)
        {
            SetUsername();
        }

        private void SetUsername()
        {
            if (!SettingsLoaded)
                return;

            Config.SetString(Config.DefaultUsername, DefaultUsername.Text);
        }
    }
}
