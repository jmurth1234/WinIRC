using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

// The MIT License (MIT)
// Copyright(c) 2016 Diederik Krols

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// Original found on Github - https://github.com/XamlBrewer/UWP-Floating-Content-Sample


namespace Rymate.Controls.UWPMenuBar
{
    /// <summary>
    /// A Content Control that can be dragged around.
    /// </summary>
    [TemplatePart(Name = BorderPartName, Type = typeof(Border))]
    [TemplatePart(Name = OverlayPartName, Type = typeof(UIElement))]
    public sealed class FloatingContent : ContentControl
    {
        private const string BorderPartName = "PART_Border";
        private const string OverlayPartName = "PART_Overlay";

        private Border border;
        private UIElement overlay;

        /// <summary>
        /// Initializes a new instance of the <see cref="FloatingContent"/> class.
        /// </summary>
        public FloatingContent()
        {
            this.DefaultStyleKey = typeof(FloatingContent);
        }

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call ApplyTemplate.
        /// In simplest terms, this means the method is called just before a UI element displays in your app.
        /// Override this method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            // Border
            this.border = this.GetTemplateChild(BorderPartName) as Border;
            if (this.border != null)
            {
                // Move Canvas properties from control to border.
                Canvas.SetLeft(this.border, Canvas.GetLeft(this));
                Canvas.SetLeft(this, 0);
                Canvas.SetTop(this.border, Canvas.GetTop(this));
                Canvas.SetTop(this, 0);

                // Move Margin to border.
                this.border.Padding = this.Margin;
                this.Margin = new Thickness(0);
            }
            else
            {
                // Exception
                throw new Exception("Floating Control Style has no Border.");
            }

            // Overlay
            this.overlay = GetTemplateChild(OverlayPartName) as UIElement;

            this.Loaded += Floating_Loaded;
        }


        private void Floating_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement el = GetClosestParentWithSize(this);
            if (el == null)
            {
                return;
            }

            el.SizeChanged += Floating_SizeChanged;
        }

        private void Floating_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var left = Canvas.GetLeft(this.border);
            var top = Canvas.GetTop(this.border);

            Rect rect = new Rect(left, top, this.border.ActualWidth, this.border.ActualHeight);
        }


        /// <summary>
        /// Gets the closest parent with a real size.
        /// </summary>
        private FrameworkElement GetClosestParentWithSize(FrameworkElement element)
        {
            while (element != null && (element.ActualHeight == 0 || element.ActualWidth == 0))
            {
                // Crawl up the Visual Tree.
                element = element.Parent as FrameworkElement;
            }

            return element;
        }
    }
}
