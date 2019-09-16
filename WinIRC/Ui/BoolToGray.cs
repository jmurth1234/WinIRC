using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace WinIRC.Ui
{
    class BoolToGrey : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var isConnected = value is bool && (bool)value;
            var isDark = Config.GetBoolean(Config.DarkTheme, true);

            var color = isDark ? Colors.White : Colors.Black;

            return new SolidColorBrush(isConnected ? color : Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new Exception("Not Implemented");
        }
    }
}
