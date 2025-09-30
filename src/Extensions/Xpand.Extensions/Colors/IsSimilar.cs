using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Xpand.Extensions.Colors {
    public static partial class ColorExtensions {
        public static bool IsSimilar(this Color newColor, List<Color> existingColors)
            => existingColors.Select(color
                => Math.Sqrt(Math.Pow(newColor.R - color.R, 2) + Math.Pow(newColor.G - color.G, 2) +
                             Math.Pow(newColor.B - color.B, 2))).Any(distance => distance < 10);
    }
}