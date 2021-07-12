using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
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
    public sealed partial class SettingsDialog : ContentDialog, INotifyPropertyChanged
    {
        public Type Type { get; private set; }
        public Action UpdateUi { get; internal set; }
        public double NavWidth { get; private set; } = double.NaN;
        public double NavHeight { get; private set; } = double.NaN;
        private CoreWindow window = CoreWindow.GetForCurrentThread();

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SettingsDialog(Type type)
        {
            this.InitializeComponent();

            this.Type = type;
            this.Loaded += ConnectView_Loaded;
            window.SizeChanged += Window_SizeChanged;
        }

        private void Window_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
        {
            CalculateWidth();
        }

        private void CalculateWidth()
        {
            var windowSize = window.Bounds;
            var width = windowSize.Width * .8;
            var height = windowSize.Height * .8;
            var margin = Header.Margin;


            if (windowSize.Width < 500)
            {
                width = double.NaN;
                height = double.NaN;
                margin.Left = 50;
            }
            else
            {
                margin.Left = 11;
            }

            if (height > 750)
            {
                height = 750;
            }

            this.NavWidth = width;
            this.NavHeight = height;
            Header.Margin = margin;

            NotifyPropertyChanged("NavWidth");
            NotifyPropertyChanged("NavHeight");
        }

        private void ConnectView_Loaded(object sender, RoutedEventArgs e)
        {
            this.CalculateWidth();
            this.Frame.Navigate(Type);
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            if (Frame.Content is BaseSettingsPage)
            {
                var settingsView = (BaseSettingsPage)Frame.Content;
                settingsView.UpdateUi += UpdateUi;

                if (settingsView != null)
                {
                    Header.Text = settingsView.Title;
                }

                Nav.SelectedItem = Nav.MenuItems.Where(x =>
                {
                    return ((string)((Framework​Element)x).Tag) == settingsView.Title;
                });
            }
            else if (Frame.Content is AboutView)
            {
                Header.Text = "About";
            }
        }

        private void Nav_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            switch (args.InvokedItem)
            {
                case "About":
                    this.Frame.Navigate(typeof(AboutView));
                    break;
                case "Appearance":
                    this.Frame.Navigate(typeof(DisplaySettingsView));
                    break;
                case "Connection":
                    this.Frame.Navigate(typeof(ConnectionSettingsView));
                    break;
                case "Behaviour":
                    this.Frame.Navigate(typeof(BehaviourSettingsView));
                    break;
                default:
                    return;
            }
        }
    }
}
