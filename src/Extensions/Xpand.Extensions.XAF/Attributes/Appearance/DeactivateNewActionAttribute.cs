using System;

namespace Xpand.Extensions.XAF.Attributes.Appearance{
    [AttributeUsage(AttributeTargets.Class)]
    public class DeactivateNewActionAttribute() :HiddenActionAttribute("New") {
        
    }
}