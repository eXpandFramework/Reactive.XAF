using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.Attributes {
    public interface IReadOnlyAttribute {
        bool AllowEdit { get; }
        bool AllowDelete { get; }
        bool AllowNew { get; }
        bool DisableListViewProcess { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ReadOnlyObjectViewAttribute:Attribute, IReadOnlyAttribute {
        public ViewType ViewType { get; }
        public bool AllowEdit { get; }
        public bool AllowDelete { get; }
        public bool AllowNew { get; }
        public bool DisableListViewProcess { get; }

        public ReadOnlyObjectViewAttribute(ViewType viewType=ViewType.Any,bool allowEdit=false,bool allowDelete=false,bool allowNew=false,bool disableListViewProcess=false) {
            ViewType = viewType;
            AllowEdit = allowEdit;
            AllowDelete = allowDelete;
            AllowNew = allowNew;
            DisableListViewProcess = disableListViewProcess;
        }
    }
}