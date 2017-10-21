using WinIRC.Ui.Brushes;

namespace WinIRC.Ui.Brushes
{ 
    public class AcrylicHostBrush : AcrylicBrushBase
    {
        public AcrylicHostBrush()
        {
        }

        protected override BackdropBrushType GetBrushType()
        {
            return BackdropBrushType.HostBackdrop;
        }
    }
}