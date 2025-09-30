using System;
using System.Drawing;

namespace Xpand.Extensions.Colors {
    public static partial class ColorExtensions {
        public static Color LightenColor(this Color color, float lighteningFactor = 1.3f)
            => Color.FromArgb(
                Math.Min(255, (int)(color.R * lighteningFactor)),
                Math.Min(255, (int)(color.G * lighteningFactor)),
                Math.Min(255, (int)(color.B * lighteningFactor))
            );
    }
}