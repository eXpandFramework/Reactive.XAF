using System;

namespace Xpand.Extensions.Windows{
    [Flags]
    public enum WindowPosition{
        None = 0,
        FullScreen = 1 << 0,
        BottomRight = 1 << 1,
        BottomLeft = 1 << 2,
        Small = 1 << 3  
    }
}