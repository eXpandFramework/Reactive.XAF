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
    public class ReadOnlyObjectViewAttribute(
        ViewType viewType = ViewType.Any,
        bool allowEdit = false,
        bool allowDelete = false,
        bool allowNew = false,
        bool disableListViewProcess = false)
        : Attribute, IReadOnlyAttribute {
        public ViewType ViewType { get; } = viewType;
        public bool AllowEdit { get; } = allowEdit;
        public bool AllowDelete { get; } = allowDelete;
        public bool AllowNew { get; } = allowNew;
        public bool DisableListViewProcess { get; } = disableListViewProcess;
    }
}