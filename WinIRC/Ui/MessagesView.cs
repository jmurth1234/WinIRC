using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace WinIRC.Ui
{
    public class MessagesView : ItemsControl
    {
        public event EventHandler ItemsChanged;
        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);
            ItemsChanged?.Invoke(this, new MessagesViewItemsChangedArgs()
            {
                Items = Items
            });
        }
    }

    public class MessagesViewItemsChangedArgs : EventArgs
    {
        public ItemCollection Items { get; internal set; }
    }
}
