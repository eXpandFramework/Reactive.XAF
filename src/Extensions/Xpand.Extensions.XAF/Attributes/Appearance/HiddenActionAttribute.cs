using System;
using DevExpress.ExpressApp.SystemModule;

namespace Xpand.Extensions.XAF.Attributes.Appearance{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Property,AllowMultiple = true)]
    public class HiddenActionAttribute(params string[] actions) : Attribute {
        public string[] Actions{ get; } = actions;
    }
}