using System;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace WinIRC.Ui.Brushes
{
    public static class CompositionExtensions
    {
        private const string TRANSLATION = "Translation";

        public static Visual GetVisual(this UIElement element)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            ElementCompositionPreview.SetIsTranslationEnabled(element, true);
            visual.Properties.InsertVector3(TRANSLATION, Vector3.Zero);
            return visual;
        }

    }
}