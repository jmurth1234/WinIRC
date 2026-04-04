using System.Security.Cryptography;
using System.Text;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinIRC.WinUI.Utils;

internal static class NickColorHelper
{
    public static SolidColorBrush CreateNickBrush(string nick, bool darkTheme)
    {
        return new SolidColorBrush(CreateNickColor(nick, darkTheme));
    }

    public static SolidColorBrush CreateNickChipBrush(string nick, bool darkTheme)
    {
        var color = CreateNickColor(nick, darkTheme);
        return new SolidColorBrush(Color.FromArgb(48, color.R, color.G, color.B));
    }

    private static Color CreateNickColor(string nick, bool darkTheme)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(nick ?? string.Empty));
        var color = Color.FromArgb(255, hash[0], hash[1], hash[2]);
        return ChangeColorBrightness(color, darkTheme ? 0.2f : -0.4f);
    }

    private static Color ChangeColorBrightness(Color color, float correctionFactor)
    {
        float red = color.R;
        float green = color.G;
        float blue = color.B;

        if (correctionFactor < 0)
        {
            correctionFactor = 1 + correctionFactor;
            red *= correctionFactor;
            green *= correctionFactor;
            blue *= correctionFactor;
        }
        else
        {
            red = (255 - red) * correctionFactor + red;
            green = (255 - green) * correctionFactor + green;
            blue = (255 - blue) * correctionFactor + blue;
        }

        return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
    }
}
