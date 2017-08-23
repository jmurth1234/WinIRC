using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Rymate.Controls.UWPMenuBar
{
    /// <summary>
    /// Button within a Menu Bar
    /// </summary>
    /// <remarks>
    /// These can either have a dropdown (like a menu bar) or are standalone
    /// buttons, which can be used for revealing a SplitView.They're also 
    /// AccessKey aware, meaning users can activate them via `alt + access key`.
    /// </remarks>
    public sealed partial class MenuBarButton : UserControl
    {
        /// <summary>
        /// Event that's called when this button is clicked
        /// </summary>
        public event TypedEventHandler<MenuBarButton, RoutedEventArgs> ButtonClicked;

        /// <summary>
        /// Event that's called when the menu is opened or closed
        /// </summary>
        public event TypedEventHandler<MenuBarButton, MenuVisiblityEventArgs> MenuVisiblityChanged;

        internal MenuBar ContainerMenu { get; set; }

        internal static readonly DependencyProperty ButtonContentPropertyField =
            DependencyProperty.Register("ButtonContent", typeof(string), typeof(MenuBarButton), new PropertyMetadata(null));

        public static DependencyProperty ButtonContentProperty
        {
            get
            {
                return ButtonContentPropertyField;
            }
        }

        internal static readonly DependencyProperty IconPropertyField =
            DependencyProperty.Register("Icon", typeof(bool), typeof(MenuBarButton), new PropertyMetadata(null));

        public static DependencyProperty IconProperty
        {
            get
            {
                return IconPropertyField;
            }
        }

        private FontFamily oldFamily;

        /// <summary>
        /// Defines whether this button displays text or an icon 
        /// </summary>
        public bool Icon
        {
            get
            {
                return ((FontFamily)GetValue(Button.FontFamilyProperty)).Equals(new FontFamily("Segoe MDL2 Assets"));
            }
            set
            {
                if (oldFamily == null)
                {
                    oldFamily = this.FontFamily;
                }

                if (value)
                {
                    Button.FontFamily = new FontFamily("Segoe MDL2 Assets");
                }
                else
                {
                    Button.FontFamily = oldFamily;
                }
            }
        }

        /// <summary>
        /// The content of this button
        /// </summary>
        public string ButtonContent
        {
            get { return (string)GetValue(ButtonContentProperty); }
            set {
                SetValue(ButtonContentProperty, value);
            }
        }

        internal static readonly DependencyProperty DisableDropdownPropertyField =
            DependencyProperty.Register("DisableDropdown", typeof(bool), typeof(MenuBarButton), new PropertyMetadata(null));

        public static DependencyProperty DisableDropdownProperty
        {
            get
            {
                return DisableDropdownPropertyField;
            }
        }


        internal static readonly DependencyProperty ChildrenPropertyField = DependencyProperty.Register(
            nameof(Content),
            typeof(UIElementCollection),
            typeof(MenuBarButton),
            new PropertyMetadata(null)
        );

        internal static readonly DependencyProperty MenuVisiblePropertyField = DependencyProperty.Register(
            "MenuVisible",
            typeof(UIElementCollection),
            typeof(bool),
            new PropertyMetadata(null)
        );

        public static DependencyProperty ChildrenProperty
        {
            get
            {
                return ChildrenPropertyField;
            }
        }

        public static DependencyProperty MenuVisibleProperty
        {
            get
            {
                return MenuVisiblePropertyField;
            }
        }

        public bool MenuVisible
        {
            get { return (bool) GetValue(MenuVisiblePropertyField); }
            private set { SetValue(MenuVisiblePropertyField, value); }
        }


        /// <summary>
        /// The content of the dropdown opened by this menu bar
        /// </summary>
        public new UIElementCollection Content
        {
            get { return (UIElementCollection)GetValue(ChildrenProperty); }
            private set { SetValue(ChildrenProperty, value); }
        }

        private int CurrentPosition;

        /// <summary>
        /// Defines whether this menu bar has a dropdown or not
        /// </summary>
        public bool DisableDropdown
        {
            get { return (bool) GetValue(DisableDropdownProperty); }
            set { SetValue(DisableDropdownProperty, value); }
        }

        public MenuBarButton()
        {
            this.InitializeComponent();
            Content = this.Menu.Children;

            (this as FrameworkElement).DataContext = this;
            this.Loaded += MenuBarButton_Loaded;
        }

        private async void MenuBarButton_Loaded(object sender, RoutedEventArgs e)
        {
            ContainerMenu = this.GetMenuBar();
            Button.Foreground = ContainerMenu.Foreground;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ButtonClicked?.Invoke(this, e);

            ToggleMenu();
        }

        internal void ToggleMenu()
        {
            if (DisableDropdown)
            {
                return;
            }

            if (CheckOther())
            {
                ContainerMenu.CurrentButton.ToggleMenu();
            }

            if (FloatingMenu.Visibility == Visibility.Collapsed)
            {
                FloatingMenu.Visibility = Visibility.Visible;

                AppearAnimation.Begin();

                if (ContainerMenu != null) ContainerMenu.CurrentButton = this;

                CurrentPosition = 0;
                var item = Menu.Children[CurrentPosition];
                ((Control)item).Focus(FocusState.Keyboard);
            }
            else
            {
                FloatingMenu.Visibility = Visibility.Collapsed;
                if (ContainerMenu != null) ContainerMenu.CurrentButton = null;
            }

            MenuVisible = FloatingMenu.Visibility == Visibility.Visible;
            MenuVisiblityChanged?.Invoke(this, new MenuVisiblityEventArgs(MenuVisible));
        }

        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (ContainerMenu == null) return;

            if (CheckOther() && e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
                ToggleMenu();
        }

        private bool CheckOther()
        {
            return ContainerMenu.CurrentButton != null && ContainerMenu.CurrentButton != this;
        }

        private void UserControl_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
        {
            args.Handled = true;

            ButtonClicked?.Invoke(this, new RoutedEventArgs());
            ToggleMenu();
        }

        private void UserControl_AccessKeyDisplayRequested(UIElement sender, AccessKeyDisplayRequestedEventArgs args)
        {
            var tooltip = ToolTipService.GetToolTip(Button) as ToolTip;

            if (tooltip == null)
            {
                tooltip = new ToolTip();
                tooltip.Background = new SolidColorBrush(Windows.UI.Colors.Black);
                tooltip.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                tooltip.Padding = new Thickness(4, 4, 4, 4);
                tooltip.VerticalOffset = -20;
                tooltip.Placement = PlacementMode.Bottom;
                ToolTipService.SetToolTip(sender, tooltip);
            }

            if (string.IsNullOrEmpty(args.PressedKeys))
            {
                tooltip.Content = sender.AccessKey;
            }
            else
            {
                tooltip.Content = sender.AccessKey.Remove(0, args.PressedKeys.Length);
            }

            tooltip.IsOpen = true;
        }

        private void UserControl_AccessKeyDisplayDismissed(UIElement sender, AccessKeyDisplayDismissedEventArgs args)
        {
            var tooltip = ToolTipService.GetToolTip(sender) as ToolTip;
            if (tooltip != null)
            {
                tooltip.IsOpen = false;
                //Fix to avoid show tooltip with mouse
                ToolTipService.SetToolTip(sender, null);
            }
        }

        private void UserControl_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (CheckOther() || FloatingMenu.Visibility == Visibility.Collapsed)
            {
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Down)
            {
                MoveDown();
            }
            else if (e.Key == Windows.System.VirtualKey.Up)
            {
                MoveUp();
            }
        }

        private void MoveDown()
        {
            if ((FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next) is Button))
            {
                return;
            } 

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);

            if (FocusManager.GetFocusedElement() is MenuFlyoutSeparator)
            {
                MoveDown();
            }
        }

        private void MoveUp()
        {
            if ((FocusManager.FindNextFocusableElement(FocusNavigationDirection.Previous) is Button))
            {
                return;
            }

            FocusManager.TryMoveFocus(FocusNavigationDirection.Previous);

            if (FocusManager.GetFocusedElement() is MenuFlyoutSeparator)
            {
                MoveUp();
            }

        }
    }
}
