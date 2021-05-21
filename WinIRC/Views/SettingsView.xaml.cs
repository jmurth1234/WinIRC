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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsView : Page
    {
        List<String> Settings = new List<string> {
            "Behaviour",
            "Appearance",
            //"Ignored Users",
            "About"
        };

        public SettingsView ()
        {
            this.InitializeComponent();

            this.SettingsList.ItemsSource = Settings;
        }

        public HeaderBlock Header { get; internal set; }

        private void SettingsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Header.Title = e.ClickedItem.ToString();
            Header.ShowBackButton = true;
            switch (e.ClickedItem.ToString())
            {
                case "Behaviour":
                    Frame.Navigate(typeof(BehaviourSettingsView));
                    break;
                case "Appearance":
                    Frame.Navigate(typeof(DisplaySettingsView));
                    break;
                case "About":
                    Frame.Navigate(typeof(AboutView));
                    break;

            }
        }
    }
}
