using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Ui;
using WinIRC.Views;

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
            "Scripts",
            //"Ignored Users",
            "About"
        };

        public SettingsView()
        {
            this.InitializeComponent();

            this.SettingsList.ItemsSource = Settings;
        }

        public HeaderBlock Header { get; internal set; }

        private async void SettingsList_ItemClick(object sender, ItemClickEventArgs e)
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
                case "Scripts":
                    CoreApplicationView newView = CoreApplication.CreateNewView();
                    int newViewId = 0;
                    await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Frame frame = new Frame();
                        frame.Navigate(typeof(ScriptsView), null);
                        Window.Current.Content = frame;
                        // You have to activate the window in order to show it later.
                        Window.Current.Activate();

                        var view = ApplicationView.GetForCurrentView();
                        newViewId = view.Id;
                        view.Title = "Manage Scripts";
                    });
                    bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
                    break;
                case "About":
                    Frame.Navigate(typeof(AboutView));
                    break;

            }
        }
    }
}
