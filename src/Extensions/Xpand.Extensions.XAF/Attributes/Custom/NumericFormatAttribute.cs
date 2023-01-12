using System;

namespace Xpand.Extensions.XAF.Attributes.Custom {
    public class NumericFormatAttribute : Attribute, ICustomAttribute {
        string ICustomAttribute.Name => "DisplayFormat;EditMask";

        string ICustomAttribute.Value => "{0:##0.#};0.000#######";
    }
}