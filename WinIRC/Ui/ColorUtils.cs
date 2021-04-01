using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace WinIRC.Ui
{
    // thanks stackoverflow
    class ColorUtils
    {
        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

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

            return Color.FromArgb(color.A, (byte) red, (byte) green, (byte) blue);
        }

        public static Color GenerateColor(string val)
        {
            var md5 = SHA1.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(val ?? ""));
            var color = Color.FromArgb(255, hash[0], hash[1], hash[2]);

            return color;
        }

        public static Color ParseColor(string hexCode)
        {
            var color = new Color();
            color.A = byte.Parse(hexCode.Substring(1, 2), NumberStyles.AllowHexSpecifier);
            color.R = byte.Parse(hexCode.Substring(3, 2), NumberStyles.AllowHexSpecifier);
            color.G = byte.Parse(hexCode.Substring(5, 2), NumberStyles.AllowHexSpecifier);
            color.B = byte.Parse(hexCode.Substring(7, 2), NumberStyles.AllowHexSpecifier);
            return color;
        }
    }
}
