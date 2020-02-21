using Microsoft.Graphics.Canvas.Text;
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
        public DisplaySettingsView()
        {
            this.InitializeComponent();
            Title = "Appearance";
            LoadSettings();
        }

        public List<string> Fonts
        {
            get
            {
                return CanvasTextFormat.GetSystemFontFamilies().OrderBy(f => f).ToList();
            }
        }

        private void LoadSettings()
        {
            FontCombo.ItemsSource = Fonts;

            if (Config.Contains(Config.DarkTheme))
            {
                this.Theme.IsOn = Config.GetBoolean(Config.DarkTheme);
            }
            else
            {
                Config.SetBoolean(Config.DarkTheme, true);
                this.Theme.IsOn = true;
            }

            if (Config.Contains(Config.Blurred))
            {
                this.BlurredBack.IsOn = Config.GetBoolean(Config.Blurred);
            }
            else
            {
                Config.SetBoolean(Config.Blurred, true);
                this.BlurredBack.IsOn = true;
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
                this.FontSize.Value = Config.GetInt(Config.FontSize, 14);
            }
            else
            {
                Config.SetInt(Config.FontSize, 14);
                this.FontSize.Value = 14;
            }

            if (Config.Contains(Config.ReducedPadding))
            {
                this.Padding.IsOn = Config.GetBoolean(Config.ReducedPadding);
            }
            else
            {
                Config.SetBoolean(Config.ReducedPadding, false);
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

            if (Config.Contains(Config.IgnoreJoinLeave))
            {
                this.IgnoreJoinLeave.IsOn = Config.GetBoolean(Config.IgnoreJoinLeave);
            }
            else
            {
                Config.SetBoolean(Config.IgnoreJoinLeave, false);
                this.IgnoreJoinLeave.IsOn = false;
            }

            if (Config.Contains(Config.ShowMetadata))
            {
                this.LinkMetadata.IsOn = Config.GetBoolean(Config.ShowMetadata);
            }
            else
            {
                Config.SetBoolean(Config.ShowMetadata, true);
                this.LinkMetadata.IsOn = true;
            }

            if (Config.Contains(Config.ModernChat))
            {
                this.ModernChatStyle.IsOn = Config.GetBoolean(Config.ModernChat);
            }
            else
            {
                Config.SetBoolean(Config.ModernChat, false);
                this.ModernChatStyle.IsOn = false;
            }

            this.SettingsLoaded = true;
        }

        private async void theme_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;

            Config.SetBoolean(Config.DarkTheme, Theme.IsOn);

            (Application.Current as App).SetTheme();
            base.UpdateUi();
        }

        private void FontCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!SettingsLoaded)
                return;
            Config.SetString(Config.FontFamily, FontCombo.SelectedValue as string);
            
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

        private void IgnoreJoinLeave_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;

            Config.SetBoolean(Config.IgnoreJoinLeave, IgnoreJoinLeave.IsOn);

            base.UpdateUi();
        }

        private void BlurredBack_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;

            Config.SetBoolean(Config.Blurred, BlurredBack.IsOn);

            base.UpdateUi();
        }

        private void LinkMetadata_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;

            Config.SetBoolean(Config.ShowMetadata, LinkMetadata.IsOn);

            base.UpdateUi();
        }

        private void FontSize_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (!SettingsLoaded)
                return;
            Config.SetInt(Config.FontSize, (int)FontSize.Value);

            base.UpdateUi();
        }

        private void ModernChatStyle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!SettingsLoaded)
                return;

            Config.SetBoolean(Config.ModernChat, ModernChatStyle.IsOn);

            base.UpdateUi();
        }
    }

    public class FontClass
    {
        public FontFamily FontFamily { get; set; }
        public string FontFamilyValue { get; set; }
    }

}
