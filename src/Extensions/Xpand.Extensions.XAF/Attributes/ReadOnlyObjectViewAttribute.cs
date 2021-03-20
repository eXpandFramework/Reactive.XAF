using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.Attributes {
    [AttributeUsage(AttributeTargets.Class)]
    public class ReadOnlyObjectViewAttribute:Attribute {
        public ViewType ViewType { get; }
        public bool AllowEdit { get; }
        public bool AllowDelete { get; }
        public bool AllowNew { get; }

        public ReadOnlyObjectViewAttribute(ViewType viewType=ViewType.Any,bool allowEdit=false,bool allowDelete=false,bool allowNew=false) {
            ViewType = viewType;
            AllowEdit = allowEdit;
            AllowDelete = allowDelete;
            AllowNew = allowNew;
        }
    }
}