using System;

namespace Xpand.Extensions.XAF.Attributes.Custom {
    public class DisplayDateAndTime : Attribute, ICustomAttribute {
        string ICustomAttribute.Name => "DisplayFormat;EditMask";

        string ICustomAttribute.Value => "{0: dd/MM/yy hh:mm:ss};dd/MM/yy hh:mm:ss";
    }
}