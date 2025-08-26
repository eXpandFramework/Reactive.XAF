using System;
using System.Collections.Generic;

namespace Xpand.Extensions.Compare;
public class CaseInsensitiveObjectComparer : IEqualityComparer<object> {
    public static readonly CaseInsensitiveObjectComparer Default = new();
    public new bool Equals(object x, object y) 
        => x is string s1 && y is string s2 ? string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase) : object.Equals(x, y);

    public int GetHashCode(object obj) 
        => obj is string s ? StringComparer.OrdinalIgnoreCase.GetHashCode(s)
            : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}