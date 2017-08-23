using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Rymate.Controls.UWPMenuBar
{
    static class Helpers
    {
        internal static MenuBar GetMenuBar(this FrameworkElement elem)
        {
            var currentElem = elem;
            while (currentElem != null && !(currentElem is MenuBar))
            {
                // Crawl up the Visual Tree.
                currentElem = VisualTreeHelper.GetParent(currentElem) as FrameworkElement;

                if (currentElem is MenuBarButton)
                {
                    currentElem = (currentElem as MenuBarButton).ContainerMenu;
                    break;
                }
            }
            return currentElem as MenuBar;
        }
    }
}
