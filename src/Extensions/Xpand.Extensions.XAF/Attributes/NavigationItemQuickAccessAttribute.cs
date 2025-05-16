using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Class)]
    public class NavigationItemQuickAccessAttribute(string viewId = null, Nesting nesting = Nesting.Any,int index=-1) : Attribute{
        public string ViewId{ get; } = viewId;
        public Nesting Nesting{ get; } = nesting;
        public int? Index{ get; } = index==-1 ? null : index;
    }
}