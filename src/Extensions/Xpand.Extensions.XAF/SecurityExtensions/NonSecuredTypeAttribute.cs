using System;

namespace Xpand.Extensions.XAF.SecurityExtensions{
    [AttributeUsage(AttributeTargets.Class)]
    public class NonSecuredTypeAttribute : Attribute{
    }
}