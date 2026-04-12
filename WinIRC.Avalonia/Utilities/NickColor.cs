using System.Security.Cryptography;
using System.Text;
using Avalonia.Media;

namespace WinIRC.Avalonia.Utilities;

public static class NickColor
{
    public static Color Generate(string value, bool darkTheme)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(value ?? ""));
        var color = Color.FromRgb(hash[0], hash[1], hash[2]);
        return AdjustBrightness(color, darkTheme ? 0.2f : -0.4f);
    }

    private static Color AdjustBrightness(Color color, float correctionFactor)
    {
        var red = color.R;
        var green = color.G;
        var blue = color.B;

        if (correctionFactor < 0)
        {
            correctionFactor = 1 + correctionFactor;
            red = (byte)(red * correctionFactor);
            green = (byte)(green * correctionFactor);
            blue = (byte)(blue * correctionFactor);
        }
        else
        {
            red = (byte)((255 - red) * correctionFactor + red);
            green = (byte)((255 - green) * correctionFactor + green);
            blue = (byte)((255 - blue) * correctionFactor + blue);
        }

        return Color.FromArgb(color.A, red, green, blue);
    }
}
