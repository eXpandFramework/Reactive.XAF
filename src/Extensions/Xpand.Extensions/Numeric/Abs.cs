using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal Abs(this decimal d) => Math.Abs(d);
    }
}