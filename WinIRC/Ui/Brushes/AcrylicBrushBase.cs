using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace WinIRC.Ui.Brushes
{
    public abstract class AcrylicBrushBase : XamlCompositionBrushBase
    {
        protected Compositor _compositor;
        protected CompositionEffectBrush _brush;

        public Color TintColor
        {
            get { return (Color)GetValue(TintColorProperty); }
            set { SetValue(TintColorProperty, value); }
        }

        public static readonly DependencyProperty TintColorProperty =
            DependencyProperty.Register("TintColor", typeof(Color), typeof(AcrylicBrushBase),
                new PropertyMetadata(null));

        public double BackdropFactor
        {
            get { return (double)GetValue(BackdropFactorProperty); }
            set { SetValue(BackdropFactorProperty, value); }
        }

        public static readonly DependencyProperty BackdropFactorProperty =
            DependencyProperty.Register("BackdropFactor", typeof(double), typeof(AcrylicBrushBase),
                new PropertyMetadata(0.5d));

        public double TintColorFactor
        {
            get { return (double)GetValue(TintColorFactorProperty); }
            set { SetValue(TintColorFactorProperty, value); }
        }

        public static readonly DependencyProperty TintColorFactorProperty =
            DependencyProperty.Register("TintColorFactor", typeof(double), typeof(AcrylicBrushBase),
                new PropertyMetadata(0.5d));

        public double BlurAmount
        {
            get { return (double)GetValue(BlurAmountProperty); }
            set { SetValue(BlurAmountProperty, value); }
        }

        public static readonly DependencyProperty BlurAmountProperty =
            DependencyProperty.Register("BlurAmount", typeof(double), typeof(AcrylicBrushBase),
                new PropertyMetadata(2d));

        public AcrylicBrushBase()
        {
        }

        protected abstract BackdropBrushType GetBrushType();

        protected override void OnConnected()
        {
            base.OnConnected();
            _compositor = Window.Current.Content.GetVisual().Compositor;
            _brush = new CompositionBrushBuilder(GetBrushType()).SetTintColor(TintColor)
                .SetBackdropFactor((float)BackdropFactor)
                .SetTintColorFactor((float)TintColorFactor)
                .SetBlurAmount((float)BlurAmount)
                .Build(_compositor);
            CompositionBrush = _brush;
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();
            _brush.Dispose();
        }
    }
}