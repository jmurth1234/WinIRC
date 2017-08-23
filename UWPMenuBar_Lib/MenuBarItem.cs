using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rymate.Controls.UWPMenuBar
{
    /// <summary>
    /// This class makes a MenuFlyoutItem work better in a menu bar.
    /// </summary>
    public sealed class MenuBarItem : MenuFlyoutItem
    {
        /// <summary>
        /// This gets the menu bar this item is part of
        /// </summary>
        public MenuBar ContainerMenu { get; private set; }

        public MenuBarItem() : base()
        {
            Padding = new Thickness(8);

            Loaded += MenuBarItem_Loaded;
            Click += MenuBarItem_Click;
        }

        private void MenuBarItem_Loaded(object sender, RoutedEventArgs e)
        {
            ContainerMenu = this.GetMenuBar();
        }

        private void MenuBarItem_Click(object sender, RoutedEventArgs e)
        {
            ContainerMenu.CurrentButton.ToggleMenu();
        }
    }

    /// <summary>
    /// This class makes a ToggleMenuFlyoutItem work better in a menu bar.
    /// </summary>
    public sealed class MenuBarToggleItem : ToggleMenuFlyoutItem
    {
        /// <summary>
        /// This gets the menu bar this item is part of
        /// </summary>
        public MenuBar ContainerMenu { get; private set; }

        public MenuBarToggleItem() : base()
        {
            Padding = new Thickness(8);

            Loaded += MenuBarItem_Loaded;
            Click += MenuBarItem_Click;
        }

        private void MenuBarItem_Loaded(object sender, RoutedEventArgs e)
        {
            ContainerMenu = this.GetMenuBar();
        }

        private void MenuBarItem_Click(object sender, RoutedEventArgs e)
        {
            ContainerMenu.CurrentButton.ToggleMenu();
        }
    }
}
