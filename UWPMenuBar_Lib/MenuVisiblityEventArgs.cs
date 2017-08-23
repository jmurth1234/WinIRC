namespace Rymate.Controls.UWPMenuBar
{
    public sealed class MenuVisiblityEventArgs
    {
        public bool IsVisible { get; private set; }

        public MenuVisiblityEventArgs(bool visible)
        {
            IsVisible = visible;
        }
    }
}