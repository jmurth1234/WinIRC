using System;
using System.Collections.Generic;
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
using WinIRC.Ui;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DisplaySettingsView : BaseSettingsPage
    {
        private bool SettingsLoaded;

        public DisplaySettingsView()
        {
            this.InitializeComponent();
            Title = "Display";

            Theme.Toggled += theme_Toggled;
            LoadSettings();
        }

        private void LoadSettings()
        {
            var fontsList = new List<String>();
            fontsList.Add( "Segoe UI" );
            fontsList.Add( "Consolas" );
            fontsList.Add( "Cambria" );
            FontCombo.ItemsSource = fontsList;

            if (Config.Contains(Config.DarkTheme))
            {
                this.Theme.IsOn = Config.GetBoolean(Config.DarkTheme);
            }
            else
            {
                Config.SetBoolean(Config.DarkTheme, true);
                this.Theme.IsOn = true;
            }

            if (Config.Contains(Config.FontFamily))
            {
                this.FontCombo.SelectedValue = Config.GetString(Config.FontFamily);
            }
            else
            {
                Config.SetString(Config.FontFamily, "Segoe UI");
                this.FontCombo.SelectedValue = "Segoe UI";
            }

            if (Config.Contains(Config.FontSize))
            {
                this.FontSize.Text = Config.GetString(Config.FontSize);
            }
            else
            {
                Config.SetString(Config.FontSize, "14");
                this.FontSize.Text = "14";
            }

            if (Config.Contains(Config.ReducedPadding))
            {
                this.Padding.IsOn = Config.GetBoolean(Config.ReducedPadding);
            }
            else
            {
                Config.SetBoolean(Config.DarkTheme, false);
                this.Padding.IsOn = false;
            }

            if (Config.Contains(Config.HideStatusBar))
            {
                this.HideStatusBar.IsOn = Config.GetBoolean(Config.HideStatusBar);
            }
            else
            {
                Config.SetBoolean(Config.HideStatusBar, false);
                this.HideStatusBar.IsOn = false;
            }

            this.SettingsLoaded = true;
        }

        private async void theme_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;
            Config.SetBoolean(Config.DarkTheme, Theme.IsOn);
            (Application.Current as App).SetTheme();

            //var dialog = new MessageDialog("To apply the theme, please restart WinIRC.");
            //await dialog.ShowAsync();
            base.UpdateUi();
        }

        private void FontCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!SettingsLoaded)
                return;
            Config.SetString(Config.FontFamily, FontCombo.SelectedValue as string);
            
            base.UpdateUi();
        }

        private void FontSize_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;
            Config.SetString(Config.FontSize, FontSize.Text);

            base.UpdateUi();
        }

        private void Padding_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;
            Config.SetBoolean(Config.ReducedPadding, Padding.IsOn);

            base.UpdateUi();
        }

        private void HideStatusBar_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;

            Config.SetBoolean(Config.HideStatusBar, HideStatusBar.IsOn);

            base.UpdateUi();
        }
    }

    public class FontClass
    {
        public FontFamily FontFamily { get; set; }
        public string FontFamilyValue { get; set; }
    }

}
