﻿using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static IEnumerable<int> ExtractNumbersFromEnd(this string input) 
            => input.Reverse().TakeWhile(char.IsDigit).Reverse().Select(c => int.Parse(c.ToString()));
    }
}