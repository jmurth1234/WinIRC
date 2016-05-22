using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace WinIRC.Ui
{
    public class BaseSettingsPage : Page
    {
        public Action UpdateUi { get; set; }

    }
}
