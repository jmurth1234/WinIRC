using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Rymate.Controls.UWPMenuBar
{
    public sealed partial class MenuBar : UserControl
    {
        /// <summary>
        /// The area of the menu bar that's used for the title label
        /// </summary>
        /// <remarks>
        /// If using this as a title bar, use this as the Window titlebar 
        /// as there's no user interactable controls here.
        /// </remarks>
        public Grid DragArea { get { return MenuBarPadding; } }

        internal MenuBarButton CurrentButton { get; set; }

        internal static readonly DependencyProperty TitlePropertyField = DependencyProperty.Register(
            "Title", 
            typeof(string), 
            typeof(MenuBar), 
            new PropertyMetadata(null)
        );

        public static DependencyProperty TitleProperty
        {
            get
            {
                return TitlePropertyField;
            }
        }

        internal static readonly DependencyProperty ChildrenPropertyField = DependencyProperty.Register(
            nameof(Content),
            typeof(UIElementCollection),
            typeof(MenuBar),
            new PropertyMetadata(null)
        );

        public static DependencyProperty ChildrenProperty
        {
            get
            {
                return ChildrenPropertyField;
            }
        }


        internal static readonly DependencyProperty TitleVisiblePropertyField = DependencyProperty.Register(
             "TitleVisible",
             typeof(Visibility),
             typeof(MenuBar),
             new PropertyMetadata(null)
        );

        public static DependencyProperty TitleVisibleProperty
        {
            get
            {
                return TitleVisiblePropertyField;
            }
        }

        /// <summary>
        /// The content of the menu bar
        /// </summary>
        public new UIElementCollection Content
        {
            get { return (UIElementCollection)GetValue(ChildrenProperty); }
            set { SetValue(ChildrenProperty, value); }
        }

        /// <summary>
        /// The title of the menu bar title area.
        /// </summary>
        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

        /// <summary>
        /// Whether the title is visible or not
        /// </summary>
        public Visibility TitleVisible
        {
            get
            {
                return (Visibility) GetValue(TitleVisibleProperty);
            }
            set
            {
                SetValue(TitleVisibleProperty, value);
            }
        }

        public MenuBar()
        {
            this.InitializeComponent();
            this.Content = MenuBarLeft.Children;

            (this as FrameworkElement).DataContext = this;
            this.Loaded += MenuBar_Loaded;
        }

        private void MenuBar_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
        }

        private void CoreWindow_PointerReleased(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
        {
            var under = VisualTreeHelper.FindElementsInHostCoordinates(args.CurrentPoint.Position, Window.Current.Content);

            var allowed = true;
            
            foreach (UIElement elem in under)
            {
                if (elem is MenuBarButton)
                {
                    allowed = false;
                    break;
                }
            }

            if (CurrentButton != null && allowed) 
            {
                CurrentButton.ToggleMenu();
            }
        }
    }
}
